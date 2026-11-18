using Microsoft.Extensions.Options;
using Moq;
using neurozen.API.IAM.Application.Internal.CommandServices;
using neurozen.API.IAM.Application.Internal.Configuration;
using neurozen.API.IAM.Domain.Model.Entities;
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
    private readonly Mock<IUserConsentRepository> _consents = new();
    private readonly List<UserConsent> _savedConsents = [];
    private readonly CapturedValue<bool> _insideTransaction = new(false);
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork;

    public UserCommandServiceSignUpTests()
    {
        _unitOfWork = TestEntities.PassThroughUnitOfWork(_insideTransaction);
        _hashing.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("$2a$12$hash-de-prueba");
        _tokens.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("jwt-de-prueba");
        _users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _professionals.Setup(p => p.ExistsByLicenseNumberAsync(It.IsAny<string>())).ReturnsAsync(false);
        _users.Setup(u => u.ExistsByDniAsync(It.IsAny<string>())).ReturnsAsync(false);
        _consents.Setup(c => c.AddAsync(It.IsAny<UserConsent>())).Callback<UserConsent>(_savedConsents.Add)
            .Returns(Task.CompletedTask);
        // Real validation rules of the Professionals context (specialty catalogue and license format).
        _professionals.Setup(p => p.ValidateRegistration(It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<ValidationErrors>()))
            .Callback<string?, string?, ValidationErrors>((specialty, license, errors) =>
                new neurozen.API.Professionals.Interfaces.ACL.Services.ProfessionalsContextFacade(
                        new Mock<neurozen.API.Professionals.Domain.Repositories.IProfessionalRepository>().Object,
                        Microsoft.Extensions.Logging.Abstractions
                            .NullLogger<neurozen.API.Professionals.Interfaces.ACL.Services.ProfessionalsContextFacade>
                            .Instance)
                    .ValidateRegistration(specialty, license, errors));
    }

    private UserCommandService CreateService() =>
        new(_users.Object, _professionals.Object, _tokens.Object, _hashing.Object, _unitOfWork.Object,
            _consents.Object, Options.Create(new LegalSettings { TermsVersion = "2026-11", PrivacyPolicyVersion = "2026-11" }));

    private static SignUpCommand Worker(string? role = "USER", bool acceptedTerms = true) =>
        new("ana@neurozen.com", "Secreta123", "Ana Torres", role, acceptedTerms, null, null, "12345678");

    private static SignUpCommand Psychologist() =>
        new("elena@psico.pe", "Segura2026", "Elena Ramírez", "PSYCHOLOGIST", true, "clinical", "45821", "87654321");

    private async Task<ValidationFailedException> AssertRejectedAsync(SignUpCommand command, string field)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => CreateService().Handle(command));
        Assert.True(exception.Errors!.ContainsKey(field),
            $"Se esperaba un error en '{field}', se obtuvo: {string.Join(", ", exception.Errors.Keys)}");
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
        return exception;
    }

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

    // ------------------------------------------------------------------ DNI, nombre, contraseña y consentimientos

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el registro ahora incluye el DNI y deja constancia separada de la
    ///   aceptación de Términos y de la Política de Privacidad (Ley 29733), con la versión vigente de cada documento.
    /// - ¿Qué hace esta prueba?: registra un USER y verifica que el DNI se guarda normalizado, que el nombre se
    ///   normaliza y que se crean dos consentimientos (TERMS y PRIVACY_POLICY) con su versión.
    /// </summary>
    [Fact]
    public async Task SignUp_Valid_ShouldStoreDniNormalizedNameAndTwoConsents()
    {
        User? saved = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>())).Callback<User>(u => saved = u).Returns(Task.CompletedTask);

        await CreateService().Handle(Worker() with
        {
            FullName = "  Ana   Torres  ", Dni = " 12345678 ", ConfirmPassword = "Secreta123",
            AcceptedPrivacyPolicy = true
        });

        Assert.Equal("12345678", saved!.Dni);
        Assert.Equal("Ana Torres", saved.FullName);
        Assert.Equal(2, _savedConsents.Count);
        Assert.Contains(_savedConsents, c => c.Type == ConsentType.Terms && c.Version == "2026-11");
        Assert.Contains(_savedConsents, c => c.Type == ConsentType.PrivacyPolicy && c.Version == "2026-11");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SignUp_EmptyName_ShouldBeRejected(string? fullName) =>
        await AssertRejectedAsync(Worker() with { FullName = fullName }, "fullName");

    [Theory]
    [InlineData("Ana Torres 2")]
    [InlineData("Ana <script>")]
    [InlineData("Ana_Torres Ruiz")]
    [InlineData("@@ ##")]
    public async Task SignUp_NameWithInvalidCharacters_ShouldBeRejected(string fullName) =>
        await AssertRejectedAsync(Worker() with { FullName = fullName }, "fullName");

    [Theory]
    [InlineData("María José Ñahui")]
    [InlineData("Jean-Pierre O'Connor")]
    [InlineData("Ana M. Torres")]
    public async Task SignUp_LegitimateNames_ShouldBeAccepted(string fullName)
    {
        var result = await CreateService().Handle(Worker() with { FullName = fullName });
        Assert.Equal(fullName, result.User.FullName);
    }

    [Fact]
    public async Task SignUp_NameTooLong_ShouldBeRejected() =>
        await AssertRejectedAsync(Worker() with { FullName = "Ana " + new string('a', 120) }, "fullName");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("1234567A")]
    [InlineData("1234-678")]
    [InlineData("1234 678")]
    [InlineData("١٢٣٤٥٦٧٨")]
    public async Task SignUp_InvalidDni_ShouldBeRejected(string? dni) =>
        await AssertRejectedAsync(Worker() with { Dni = dni }, "dni");

    [Fact]
    public async Task SignUp_DuplicateDni_ShouldThrowConflict()
    {
        _users.Setup(u => u.ExistsByDniAsync("12345678")).ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => CreateService().Handle(Worker()));

        Assert.Equal("Ya existe una cuenta con este DNI", exception.Message);
        Assert.DoesNotContain("12345678", exception.Message);
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData("ana@")]
    [InlineData("ana.neurozen.com")]
    [InlineData("")]
    public async Task SignUp_InvalidEmail_ShouldBeRejected(string email) =>
        await AssertRejectedAsync(Worker() with { Email = email }, "email");

    [Fact]
    public async Task SignUp_EmailIsNormalized()
    {
        var result = await CreateService().Handle(Worker() with { Email = "  Ana@NeuroZen.COM " });
        Assert.Equal("ana@neurozen.com", result.User.Email);
    }

    [Theory]
    [InlineData("Abc1234")]      // 7 caracteres
    [InlineData("SoloLetras")]   // sin números
    [InlineData("12345678")]     // sin letras
    public async Task SignUp_WeakPassword_ShouldBeRejected(string password) =>
        await AssertRejectedAsync(Worker() with { Password = password }, "password");

    [Fact]
    public async Task SignUp_ConfirmPasswordMismatch_ShouldBeRejected() =>
        await AssertRejectedAsync(Worker() with { ConfirmPassword = "OtraClave99" }, "confirmPassword");

    [Fact]
    public async Task SignUp_PrivacyPolicyExplicitlyRejected_ShouldBeRejected()
    {
        await AssertRejectedAsync(Worker() with { AcceptedPrivacyPolicy = false }, "acceptedPrivacyPolicy");
        Assert.Empty(_savedConsents);
    }

    [Fact]
    public async Task SignUp_PasswordIsHashed_AndNeverStoredInPlainText()
    {
        User? saved = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>())).Callback<User>(u => saved = u).Returns(Task.CompletedTask);

        await CreateService().Handle(Worker() with { ConfirmPassword = "Secreta123" });

        Assert.Equal("$2a$12$hash-de-prueba", saved!.PasswordHash);
        _hashing.Verify(h => h.HashPassword("Secreta123"), Times.Once);
    }

    // ------------------------------------------------------------------ Registro de psicólogos

    [Fact]
    public async Task SignUp_Psychologist_InvalidDni_ShouldBeRejected() =>
        await AssertRejectedAsync(Psychologist() with { Dni = "8765432X" }, "dni");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SignUp_Psychologist_EmptySpecialty_ShouldBeRejected(string? specialty) =>
        await AssertRejectedAsync(Psychologist() with { Specialty = specialty }, "specialty");

    [Theory]
    [InlineData("astrologia")]
    [InlineData("Psicología clínica")]
    [InlineData("<b>clinical</b>")]
    public async Task SignUp_Psychologist_SpecialtyNotInCatalogue_ShouldBeRejected(string specialty) =>
        await AssertRejectedAsync(Psychologist() with { Specialty = specialty }, "specialty");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("1234567")]
    [InlineData("45A21")]
    public async Task SignUp_Psychologist_InvalidLicense_ShouldBeRejected(string? license) =>
        await AssertRejectedAsync(Psychologist() with { LicenseNumber = license }, "licenseNumber");

    [Fact]
    public async Task SignUp_Psychologist_InvalidPassword_ShouldBeRejected() =>
        await AssertRejectedAsync(Psychologist() with { Password = "corta1" }, "password");

    [Fact]
    public async Task SignUp_Psychologist_DuplicateEmail_ShouldThrowConflict()
    {
        _users.Setup(u => u.ExistsByEmailAsync("elena@psico.pe")).ReturnsAsync(true);
        await Assert.ThrowsAsync<ConflictException>(() => CreateService().Handle(Psychologist()));
        _professionals.Verify(p => p.RegisterPendingProfessionalAsync(It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("clinical")]
    [InlineData("organizational")]
    [InlineData("educational")]
    [InlineData("family")]
    [InlineData("childAdolescent")]
    [InlineData("health")]
    [InlineData("social")]
    [InlineData("neuropsychology")]
    public async Task SignUp_Psychologist_EveryAllowedSpecialty_ShouldBeAccepted(string specialty)
    {
        var result = await CreateService().Handle(Psychologist() with { Specialty = specialty });
        Assert.Equal(UserRole.Psychologist, result.User.Role);
        _professionals.Verify(p => p.RegisterPendingProfessionalAsync(It.IsAny<long>(), specialty, "45821"),
            Times.Once);
    }
}
