using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using neurozen.API.IAM.Application.Internal.CommandServices;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Services;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.IAM;

public class UserCommandServiceSignInTests
{
    private const string TestSecret = "clave-de-prueba-solo-para-tests-0123456789-abcdef";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IHashingService> _hashing = new();

    private UserCommandService CreateService(ITokenService tokenService) =>
        new(_users.Object, new Mock<IProfessionalsContextFacade>().Object, tokenService, _hashing.Object,
            new Mock<IUnitOfWork>().Object);

    private static TokenService RealTokenService() =>
        new(Options.Create(new TokenSettings
        {
            Secret = TestSecret, Issuer = "neurozen-api", Audience = "neurozen-web", ExpirationMinutes = 120
        }));

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el frontend y la autorización dependen de que el JWT traiga el id del
    ///   usuario (sub), su rol y una expiración; sin ellos no hay control de acceso.
    /// - ¿Qué hace esta prueba?: inicia sesión con credenciales válidas usando el TokenService real y lee el token
    ///   para comprobar sub, role y exp (alrededor de 2 horas).
    /// </summary>
    [Fact]
    public async Task SignIn_ValidCredentials_ShouldReturnTokenWithSubRoleAndExp()
    {
        // Arrange
        var user = new User("ana@neurozen.com", "hash", "Ana Torres", UserRole.User, DateTime.UtcNow).WithId(42);
        _users.Setup(u => u.FindByEmailAsync("ana@neurozen.com")).ReturnsAsync(user);
        _hashing.Setup(h => h.VerifyPassword("Secreta123", "hash")).Returns(true);
        var service = CreateService(RealTokenService());

        // Act
        var result = await service.Handle(new SignInCommand("Ana@NeuroZen.com ", "Secreta123"));

        // Assert
        var token = new JsonWebTokenHandler().ReadJsonWebToken(result.Token);
        Assert.Equal("42", token.Subject);
        Assert.Equal("USER", token.GetClaim("role").Value);
        Assert.InRange(token.ValidTo, DateTime.UtcNow.AddMinutes(115), DateTime.UtcNow.AddMinutes(121));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: enumeración de cuentas; un atacante no debe poder saber si un correo está
    ///   registrado por el mensaje ni por el tiempo de respuesta.
    /// - ¿Qué hace esta prueba?: prueba una contraseña incorrecta y un correo inexistente; ambos lanzan el mismo
    ///   error y en ambos se ejecuta la verificación de la contraseña (también contra un hash ficticio).
    /// </summary>
    [Fact]
    public async Task SignIn_WrongPasswordOrUnknownEmail_ShouldReturnSameError()
    {
        // Arrange
        var user = new User("ana@neurozen.com", "hash", "Ana Torres", UserRole.User, DateTime.UtcNow).WithId(42);
        _users.Setup(u => u.FindByEmailAsync("ana@neurozen.com")).ReturnsAsync(user);
        _users.Setup(u => u.FindByEmailAsync("nadie@neurozen.com")).ReturnsAsync((User?)null);
        _hashing.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string?>())).Returns(false);
        var tokens = new Mock<ITokenService>();
        var service = CreateService(tokens.Object);

        // Act
        var wrongPassword = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.Handle(new SignInCommand("ana@neurozen.com", "Incorrecta1")));
        var unknownEmail = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.Handle(new SignInCommand("nadie@neurozen.com", "Incorrecta1")));

        // Assert
        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
        _hashing.Verify(h => h.VerifyPassword("Incorrecta1", "hash"), Times.Once);
        _hashing.Verify(h => h.VerifyPassword("Incorrecta1", null), Times.Once);
        tokens.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
