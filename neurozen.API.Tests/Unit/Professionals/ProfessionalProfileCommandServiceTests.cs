using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Application.Internal.CommandServices;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Professionals;

public class ProfessionalProfileCommandServiceTests
{
    private const long UserId = 20;
    private readonly Mock<IProfessionalRepository> _professionals = new();
    private readonly Mock<IIamContextFacade> _iam = new();
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork =
        TestEntities.PassThroughUnitOfWork();

    public ProfessionalProfileCommandServiceTests()
    {
        _iam.Setup(i => i.FetchUserSummaryAsync(UserId))
            .ReturnsAsync(new UserSummary(UserId, "Elena Ramírez", "elena@psico.pe", false));
        _iam.Setup(i => i.ValidateFullName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidationErrors>()));
        _professionals.Setup(p => p.ExistsByLicenseNumberForOtherAsync(It.IsAny<string>(), It.IsAny<long>()))
            .ReturnsAsync(false);
    }

    private ProfessionalProfileCommandService CreateService(Professional professional)
    {
        _professionals.Setup(p => p.FindByUserIdAsync(UserId)).ReturnsAsync(professional);
        return new ProfessionalProfileCommandService(_professionals.Object, _iam.Object, _unitOfWork.Object,
            NullLogger<ProfessionalProfileCommandService>.Instance);
    }

    private static UpdateMyProfessionalProfileCommand Profile(string license = "45821", string? bio = "Bio",
        decimal? price = 80m, string start = "09:00", string end = "13:00") =>
        new(UserId, "Elena Ramírez", "clinical", license, bio, 8, price, true,
            ["monday", "wednesday"], start, end, null);

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: lo que el admin verificó es la colegiatura; si el psicólogo la cambia,
    ///   la verificación anterior ya no vale y debe volver a revisión (PENDING) antes de seguir atendiendo.
    /// - ¿Qué hace esta prueba?: un psicólogo VERIFIED cambia su colegiatura y se comprueba que queda PENDING y sin
    ///   los datos de la verificación previa.
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_ChangingLicense_ShouldReturnToPending()
    {
        // Arrange
        var professional = TestProfessionals.Verified(UserId).WithId(5);
        var service = CreateService(professional);

        // Act
        await service.Handle(Profile(license: "45822"));

        // Assert
        Assert.Equal(VerificationStatus.Pending, professional.VerificationStatus);
        Assert.Equal("45822", professional.LicenseNumber);
        Assert.Null(professional.VerifiedBy);
        Assert.Null(professional.VerifiedAt);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un psicólogo rechazado debe poder corregir y reenviar su solicitud; al
    ///   guardar el perfil vuelve a PENDING y el motivo anterior desaparece.
    /// - ¿Qué hace esta prueba?: con un perfil REJECTED guarda el perfil sin cambiar colegiatura ni nombre y comprueba
    ///   el estado PENDING y el motivo vacío.
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_Resubmit_AfterRejected_ShouldReturnToPending()
    {
        // Arrange
        var professional = TestProfessionals.Pending(UserId).WithId(5);
        professional.Reject("La colegiatura no figura como HABILITADO", DateTime.UtcNow);
        var service = CreateService(professional);

        // Act
        await service.Handle(Profile());

        // Assert
        Assert.Equal(VerificationStatus.Pending, professional.VerificationStatus);
        Assert.Null(professional.RejectionReason);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el perfil es público; una presentación de más de 800 caracteres, una tarifa
    ///   mayor a 1000 o un horario cuya hora de fin no es posterior al inicio romperían el directorio y las reservas.
    /// - ¿Qué hace esta prueba?: envía los tres valores fuera de rango y comprueba un 400 con los tres campos, sin
    ///   modificar el perfil ni guardar.
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_InvalidRanges_ShouldThrowValidation()
    {
        // Arrange
        var professional = TestProfessionals.Verified(UserId).WithId(5);
        var originalBio = professional.Bio;
        var service = CreateService(professional);

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => service.Handle(
            Profile(bio: new string('a', 801), price: 1001m, start: "13:00", end: "09:00")));

        // Assert
        Assert.True(exception.Errors!.ContainsKey("bio"));
        Assert.True(exception.Errors.ContainsKey("price"));
        Assert.True(exception.Errors.ContainsKey("availabilitySchedule.end"));
        Assert.Equal(originalBio, professional.Bio);
        Assert.Equal(VerificationStatus.Verified, professional.VerificationStatus);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }
}
