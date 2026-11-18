using Moq;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.IAM.Application.Internal.CommandServices;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Shared.Application.Accounts;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.IAM;

public class UserAccountCommandServiceTests
{
    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: con la sesión iniciada, una contraseña actual incorrecta es un error de
    ///   datos (400), no de autenticación; un 401 haría que el frontend cerrara la sesión del usuario.
    /// - ¿Qué hace esta prueba?: cambia la contraseña con la actual incorrecta y comprueba que lanza exactamente
    ///   ValidationFailedException con el campo currentPassword, y que no se hashea ni se guarda nada.
    /// </summary>
    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ShouldThrowValidationNotUnauthorized()
    {
        // Arrange
        var user = new User("ana@neurozen.com", "hash-actual", "Ana Torres", UserRole.User, DateTime.UtcNow)
            .WithId(7);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.FindByIdAsync(7)).ReturnsAsync(user);
        var hashing = new Mock<IHashingService>();
        hashing.Setup(h => h.VerifyPassword("Incorrecta1", "hash-actual")).Returns(false);
        var unitOfWork = TestEntities.PassThroughUnitOfWork();
        var service = new UserAccountCommandService(users.Object, hashing.Object, Array.Empty<IUserDataEraser>(),
            unitOfWork.Object, new Mock<IProfessionalsContextFacade>().Object);

        // Act
        var exception = await Record.ExceptionAsync(
            () => service.Handle(new ChangePasswordCommand(7, "Incorrecta1", "NuevaClave2026")));

        // Assert
        var validation = Assert.IsType<ValidationFailedException>(exception);
        Assert.True(validation.Errors!.ContainsKey("currentPassword"));
        Assert.Equal("hash-actual", user.PasswordHash);
        hashing.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_EmailChangeWithoutCurrentPassword_ShouldThrowValidation()
    {
        var user = new User("ana@neurozen.com", "hash-actual", "Ana Torres", UserRole.User, DateTime.UtcNow)
            .WithId(7);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.FindByIdAsync(7)).ReturnsAsync(user);
        users.Setup(u => u.ExistsByEmailForOtherUserAsync("nuevo@neurozen.com", 7)).ReturnsAsync(false);
        var hashing = new Mock<IHashingService>();
        hashing.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var unitOfWork = TestEntities.PassThroughUnitOfWork();
        var service = new UserAccountCommandService(users.Object, hashing.Object, Array.Empty<IUserDataEraser>(),
            unitOfWork.Object, new Mock<IProfessionalsContextFacade>().Object);

        var exception = await Record.ExceptionAsync(
            () => service.Handle(new UpdateUserProfileCommand(7, "nuevo@neurozen.com", "Ana Torres", null, null,
                null, null, null, null, null, null)));

        var validation = Assert.IsType<ValidationFailedException>(exception);
        Assert.True(validation.Errors!.ContainsKey("currentPassword"));
        unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el nombre de un psicólogo es lo que el admin coteja con el CPsP; si cambia
    ///   por PUT /users/{id} el perfil debe volver a PENDING (antes solo ocurría por PUT /professionals/me).
    /// - ¿Qué hace esta prueba?: un PSYCHOLOGIST cambia su nombre y se verifica la llamada al contexto Professionals;
    ///   un USER que cambia su nombre no la dispara.
    /// </summary>
    [Theory]
    [InlineData(UserRole.Psychologist, 1)]
    [InlineData(UserRole.User, 0)]
    public async Task UpdateProfile_NameChange_ReturnsPsychologistToPending(UserRole role, int expectedCalls)
    {
        var user = new User("elena@psico.pe", "hash", "Elena Ramírez", role, DateTime.UtcNow).WithId(9);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.FindByIdAsync(9)).ReturnsAsync(user);
        var professionals = new Mock<IProfessionalsContextFacade>();
        var service = new UserAccountCommandService(users.Object, new Mock<IHashingService>().Object,
            Array.Empty<IUserDataEraser>(), TestEntities.PassThroughUnitOfWork().Object, professionals.Object);

        await service.Handle(new UpdateUserProfileCommand(9, null, "  Elena   Ramírez Torres ", null, null, null,
            null, null, null, null));

        Assert.Equal("Elena Ramírez Torres", user.FullName);
        professionals.Verify(p => p.ReturnToPendingVerificationAsync(9), Times.Exactly(expectedCalls));
    }

    [Fact]
    public async Task UpdateProfile_SameNameWithExtraSpaces_DoesNotReturnPsychologistToPending()
    {
        var user = new User("elena@psico.pe", "hash", "Elena Ramírez", UserRole.Psychologist, DateTime.UtcNow)
            .WithId(9);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.FindByIdAsync(9)).ReturnsAsync(user);
        var professionals = new Mock<IProfessionalsContextFacade>();
        var service = new UserAccountCommandService(users.Object, new Mock<IHashingService>().Object,
            Array.Empty<IUserDataEraser>(), TestEntities.PassThroughUnitOfWork().Object, professionals.Object);

        await service.Handle(new UpdateUserProfileCommand(9, null, " Elena  Ramírez ", null, null, null, null, null,
            null, null));

        professionals.Verify(p => p.ReturnToPendingVerificationAsync(It.IsAny<long>()), Times.Never);
    }

    [Theory]
    [InlineData("2000-01-01xyz")]
    [InlineData("01/01/2000")]
    public async Task UpdateProfile_MalformedDateOfBirth_ShouldThrowValidation(string dateOfBirth)
    {
        var user = new User("ana@neurozen.com", "hash", "Ana Torres", UserRole.User, DateTime.UtcNow).WithId(7);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.FindByIdAsync(7)).ReturnsAsync(user);
        var service = new UserAccountCommandService(users.Object, new Mock<IHashingService>().Object,
            Array.Empty<IUserDataEraser>(), TestEntities.PassThroughUnitOfWork().Object,
            new Mock<IProfessionalsContextFacade>().Object);

        var exception = await Record.ExceptionAsync(() => service.Handle(new UpdateUserProfileCommand(7, null, null,
            null, null, null, dateOfBirth, null, null, null)));

        Assert.True(Assert.IsType<ValidationFailedException>(exception).Errors!.ContainsKey("dateOfBirth"));
    }

    [Fact]
    public void Anonymize_ShouldClearDni()
    {
        var user = new User("ana@neurozen.com", "hash", "Ana Torres", UserRole.User, DateTime.UtcNow, "12345678")
            .WithId(7);

        user.Anonymize(DateTime.UtcNow);

        Assert.Null(user.Dni);
    }
}
