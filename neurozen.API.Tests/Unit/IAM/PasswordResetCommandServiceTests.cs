using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Moq;
using neurozen.API.IAM.Application.Internal.CommandServices;
using neurozen.API.IAM.Application.Internal.Configuration;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.IAM;

public class PasswordResetCommandServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordResetTokenRepository> _tokens = new();
    private readonly Mock<IHashingService> _hashing = new();
    private readonly Mock<IEmailQueue> _emailQueue = new();
    private readonly Mock<IPasswordResetThrottle> _throttle = new();
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork =
        TestEntities.PassThroughUnitOfWork();

    public PasswordResetCommandServiceTests()
    {
        _throttle.Setup(t => t.TryAcquire(It.IsAny<string>())).Returns(true);
        _emailQueue.Setup(q => q.EnqueueAsync(It.IsAny<EmailMessage>())).Returns(ValueTask.CompletedTask);
        _tokens.Setup(t => t.ListUnusedByUserIdAsync(It.IsAny<long>())).ReturnsAsync(new List<PasswordResetToken>());
    }

    private PasswordResetCommandService CreateService() =>
        new(_users.Object, _tokens.Object, _hashing.Object, _emailQueue.Object, _throttle.Object,
            Options.Create(new PasswordResetSettings { FrontendBaseUrl = "http://localhost:5173" }),
            _unitOfWork.Object);

    private static User Ana() =>
        new User("ana@neurozen.com", "hash-actual", "Ana Torres", UserRole.User, DateTime.UtcNow).WithId(7);

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: «Olvidé mi contraseña» no debe revelar qué correos están registrados;
    ///   con un correo desconocido responde igual, sin error, y sin enviar nada.
    /// - ¿Qué hace esta prueba?: pide la recuperación para un correo inexistente y comprueba que no lanza error,
    ///   que no crea token y que el envío de correo no se llama nunca.
    /// </summary>
    [Fact]
    public async Task ForgotPassword_UnknownEmail_ShouldNotFailNorSendEmail()
    {
        // Arrange
        _users.Setup(u => u.FindByEmailAsync("nadie@neurozen.com")).ReturnsAsync((User?)null);
        var service = CreateService();

        // Act
        var exception = await Record.ExceptionAsync(() => service.Handle(new ForgotPasswordCommand("nadie@neurozen.com")));

        // Assert
        Assert.Null(exception);
        _tokens.Verify(t => t.AddAsync(It.IsAny<PasswordResetToken>()), Times.Never);
        _emailQueue.Verify(q => q.EnqueueAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: si la base se filtra, los enlaces de recuperación no deben servir; se
    ///   guarda solo el hash SHA-256 del token, el valor real viaja únicamente en el correo, y se envía una sola vez.
    /// - ¿Qué hace esta prueba?: pide la recuperación para un correo existente, captura el token guardado y el correo
    ///   enviado, y comprueba que lo guardado es el hash del token del enlace (nunca el token), que vence en una
    ///   hora como máximo y que el correo se envió una vez al usuario.
    /// </summary>
    [Fact]
    public async Task ForgotPassword_KnownEmail_ShouldStoreHashedTokenAndSendOnce()
    {
        // Arrange
        _users.Setup(u => u.FindByEmailAsync("ana@neurozen.com")).ReturnsAsync(Ana());
        PasswordResetToken? stored = null;
        EmailMessage? sent = null;
        _tokens.Setup(t => t.AddAsync(It.IsAny<PasswordResetToken>()))
            .Callback<PasswordResetToken>(t => stored = t).Returns(Task.CompletedTask);
        _emailQueue.Setup(q => q.EnqueueAsync(It.IsAny<EmailMessage>()))
            .Callback<EmailMessage>(m => sent = m).Returns(ValueTask.CompletedTask);
        var service = CreateService();

        // Act
        await service.Handle(new ForgotPasswordCommand("ana@neurozen.com"));

        // Assert
        Assert.NotNull(stored);
        Assert.NotNull(sent);
        var rawToken = Uri.UnescapeDataString(Regex.Match(sent!.TextBody, @"token=([^\s]+)").Groups[1].Value);
        Assert.False(string.IsNullOrEmpty(rawToken));
        Assert.Equal(Sha256Hex(rawToken), stored!.TokenHash);
        Assert.NotEqual(rawToken, stored.TokenHash);
        Assert.True(stored.ExpiresAt <= DateTime.UtcNow.AddMinutes(60));
        Assert.Equal("ana@neurozen.com", sent.To);
        _emailQueue.Verify(q => q.EnqueueAsync(It.IsAny<EmailMessage>()), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: con un enlace válido el usuario debe poder elegir una nueva contraseña, y
    ///   el enlace debe quedar usado para que no sirva una segunda vez.
    /// - ¿Qué hace esta prueba?: restablece con un token vigente y comprueba que el hash de la contraseña cambió,
    ///   que el token quedó marcado como usado y que se guardaron los cambios.
    /// </summary>
    [Fact]
    public async Task ResetPassword_ValidToken_ShouldChangePasswordAndMarkTokenUsed()
    {
        // Arrange
        var user = Ana();
        var token = new PasswordResetToken(user.Id, Sha256Hex("token-valido"), DateTime.UtcNow.AddMinutes(30));
        _tokens.Setup(t => t.FindByTokenHashAsync(Sha256Hex("token-valido"))).ReturnsAsync(token);
        _tokens.Setup(t => t.ListUnusedByUserIdAsync(user.Id)).ReturnsAsync(new List<PasswordResetToken> { token });
        _users.Setup(u => u.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _hashing.Setup(h => h.HashPassword("NuevaClave2026")).Returns("hash-nuevo");
        var service = CreateService();

        // Act
        await service.Handle(new ResetPasswordCommand("token-valido", "NuevaClave2026"));

        // Assert
        Assert.Equal("hash-nuevo", user.PasswordHash);
        Assert.NotNull(token.UsedAt);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un enlace vencido (más de 1 hora) o ya usado no debe permitir cambiar la
    ///   contraseña; si no, un correo antiguo o reenviado daría acceso a la cuenta.
    /// - ¿Qué hace esta prueba?: intenta restablecer con un token vencido y con uno ya usado; ambos responden 400
    ///   «El enlace no es válido o ya venció» y no se cambia ni se guarda nada.
    /// </summary>
    [Theory]
    [InlineData("vencido")]
    [InlineData("usado")]
    public async Task ResetPassword_ExpiredOrUsedToken_ShouldBeRejected(string state)
    {
        // Arrange
        var token = state == "vencido"
            ? new PasswordResetToken(7, Sha256Hex("token-x"), DateTime.UtcNow.AddMinutes(-1))
            : new PasswordResetToken(7, Sha256Hex("token-x"), DateTime.UtcNow.AddMinutes(30));
        if (state == "usado") token.MarkUsed(DateTime.UtcNow.AddMinutes(-5));
        _tokens.Setup(t => t.FindByTokenHashAsync(Sha256Hex("token-x"))).ReturnsAsync(token);
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => service.Handle(new ResetPasswordCommand("token-x", "NuevaClave2026")));

        // Assert
        Assert.Equal("El enlace no es válido o ya venció", exception.Message);
        _hashing.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }
}
