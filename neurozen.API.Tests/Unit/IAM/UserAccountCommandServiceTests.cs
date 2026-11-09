using Moq;
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
            unitOfWork.Object);

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
            unitOfWork.Object);

        var exception = await Record.ExceptionAsync(
            () => service.Handle(new UpdateUserProfileCommand(7, "nuevo@neurozen.com", "Ana Torres", null, null,
                null, null, null, null, null, null)));

        var validation = Assert.IsType<ValidationFailedException>(exception);
        Assert.True(validation.Errors!.ContainsKey("currentPassword"));
        unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }
}
