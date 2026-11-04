using Moq;
using neurozen.API.IAM.Application.Internal.CommandServices;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.IAM;

public class UserCommandServiceSignUpTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProfessionalsContextFacade> _professionals = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IHashingService> _hashing = new();
    private readonly CapturedValue<bool> _insideTransaction = new(false);
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork;

    public UserCommandServiceSignUpTests()
    {
        _unitOfWork = TestEntities.PassThroughUnitOfWork(_insideTransaction);
        _hashing.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("$2a$12$hash-de-prueba");
        _tokens.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("jwt-de-prueba");
        _users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _professionals.Setup(p => p.ExistsByLicenseNumberAsync(It.IsAny<string>())).ReturnsAsync(false);
    }

    private UserCommandService CreateService() =>
        new(_users.Object, _professionals.Object, _tokens.Object, _hashing.Object, _unitOfWork.Object);

    private static SignUpCommand Worker(string? role = "USER", bool acceptedTerms = true) =>
        new("ana@neurozen.com", "Secreta123", "Ana Torres", role, acceptedTerms, null, null);

    private static SignUpCommand Psychologist() =>
        new("elena@psico.pe", "Segura2026", "Elena Ramírez", "PSYCHOLOGIST", true, "clinical", "45821");

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un trabajador que se registra debe quedar con rol USER y recibir su token,
    ///   sin crear ningún perfil profesional.
    /// - ¿Qué hace esta prueba?: registra un USER y verifica el rol guardado, el token devuelto y que no se llamó
    ///   al registro de profesionales.
    /// </summary>
    [Fact]
    public async Task SignUp_Worker_ShouldCreateUserWithUserRole()
    {
        // Arrange
        User? saved = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>())).Callback<User>(u => saved = u).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.Handle(Worker());

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(UserRole.User, saved!.Role);
        Assert.Equal("ana@neurozen.com", saved.Email);
        Assert.Equal("jwt-de-prueba", result.Token);
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Once);
        _professionals.Verify(p => p.RegisterPendingProfessionalAsync(It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un psicólogo no puede quedar con cuenta pero sin perfil profesional;
    ///   usuario y perfil PENDING deben crearse juntos en la misma transacción.
    /// - ¿Qué hace esta prueba?: registra un PSYCHOLOGIST y comprueba que el usuario y el perfil (con su especialidad
    ///   y colegiatura) se crean dentro de la transacción, una sola vez cada uno.
    /// </summary>
    [Fact]
    public async Task SignUp_Psychologist_ShouldCreateProfessionalInPendingInSameTransaction()
    {
        // Arrange
        var userAddedInTransaction = false;
        var professionalAddedInTransaction = false;
        _users.Setup(u => u.AddAsync(It.IsAny<User>()))
            .Callback(() => userAddedInTransaction = _insideTransaction.Value)
            .Returns(Task.CompletedTask);
        _professionals.Setup(p => p.RegisterPendingProfessionalAsync(It.IsAny<long>(), "clinical", "45821"))
            .Callback(() => professionalAddedInTransaction = _insideTransaction.Value)
            .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        var result = await service.Handle(Psychologist());

        // Assert
        Assert.Equal(UserRole.Psychologist, result.User.Role);
        Assert.True(userAddedInTransaction);
        Assert.True(professionalAddedInTransaction);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _professionals.Verify(p => p.RegisterPendingProfessionalAsync(It.IsAny<long>(), "clinical", "45821"),
            Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: escalada de privilegios; nadie puede crearse una cuenta ADMIN desde el
    ///   registro público.
    /// - ¿Qué hace esta prueba?: intenta registrarse con rol ADMIN y comprueba el 400 en el campo role y que no se
    ///   guardó ningún usuario.
    /// </summary>
    [Fact]
    public async Task SignUp_WithAdminRole_ShouldThrowValidation()
    {
        // Arrange
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => service.Handle(Worker("ADMIN")));

        // Assert
        Assert.True(exception.Errors!.ContainsKey("role"));
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: Ley 29733; no se puede crear la cuenta sin que el usuario acepte los
    ///   Términos y la Política de Privacidad.
    /// - ¿Qué hace esta prueba?: registra sin aceptar los términos y comprueba el error en acceptedTerms y que no se
    ///   guardó nada.
    /// </summary>
    [Fact]
    public async Task SignUp_WithoutAcceptedTerms_ShouldThrowValidation()
    {
        // Arrange
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => service.Handle(Worker(acceptedTerms: false)));

        // Assert
        Assert.True(exception.Errors!.ContainsKey("acceptedTerms"));
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el correo es el login; dos cuentas con el mismo correo romperían el
    ///   inicio de sesión.
    /// - ¿Qué hace esta prueba?: simula un correo ya registrado y comprueba el 409 con su mensaje y que no se guardó
    ///   otro usuario.
    /// </summary>
    [Fact]
    public async Task SignUp_DuplicateEmail_ShouldThrowConflict()
    {
        // Arrange
        _users.Setup(u => u.ExistsByEmailAsync("ana@neurozen.com")).ReturnsAsync(true);
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.Handle(Worker()));

        // Assert
        Assert.Equal("Ya existe una cuenta con este correo", exception.Message);
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: una colegiatura identifica a un solo psicólogo del CPsP; no puede usarse
    ///   en dos cuentas, y el frontend muestra el error en el campo de colegiatura.
    /// - ¿Qué hace esta prueba?: simula una colegiatura ya registrada y comprueba el 409 cuyo mensaje menciona la
    ///   colegiatura, sin crear usuario ni perfil.
    /// </summary>
    [Fact]
    public async Task SignUp_DuplicateLicense_ShouldThrowConflictMentioningColegiatura()
    {
        // Arrange
        _professionals.Setup(p => p.ExistsByLicenseNumberAsync("45821")).ReturnsAsync(true);
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.Handle(Psychologist()));

        // Assert
        Assert.Contains("colegiatura", exception.Message);
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
        _professionals.Verify(p => p.RegisterPendingProfessionalAsync(It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }
}
