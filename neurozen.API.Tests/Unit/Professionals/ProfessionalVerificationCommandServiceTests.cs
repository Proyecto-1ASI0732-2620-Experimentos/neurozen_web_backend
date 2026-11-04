using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Application.Internal.CommandServices;
using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.Entities;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Professionals;

public class ProfessionalVerificationCommandServiceTests
{
    private const long AdminId = 99;
    private readonly Mock<IProfessionalRepository> _professionals = new();
    private readonly Mock<IVerificationAuditRepository> _audits = new();
    private readonly Mock<IIamContextFacade> _iam = new();
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork =
        TestEntities.PassThroughUnitOfWork();

    public ProfessionalVerificationCommandServiceTests()
    {
        _iam.Setup(i => i.FetchUserSummaryAsync(It.IsAny<long>()))
            .ReturnsAsync(new UserSummary(20, "Elena Ramírez", "elena@psico.pe", false));
    }

    private ProfessionalVerificationCommandService CreateService() =>
        new(_professionals.Object, _audits.Object, _iam.Object, _unitOfWork.Object,
            NullLogger<ProfessionalVerificationCommandService>.Instance);

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: solo un admin puede habilitar a un psicólogo tras validar su colegiatura en
    ///   el CPsP, y la decisión debe quedar auditada (quién, cuándo, a quién).
    /// - ¿Qué hace esta prueba?: verifica un perfil PENDING y comprueba el estado VERIFIED, verifiedBy/verifiedAt y
    ///   el registro de auditoría con la acción, el admin y el profesional.
    /// </summary>
    [Fact]
    public async Task Verify_PendingProfessional_ShouldSetVerifiedWithAuditData()
    {
        // Arrange
        var professional = TestProfessionals.Pending().WithId(5);
        _professionals.Setup(p => p.FindByIdAsync(5)).ReturnsAsync(professional);
        VerificationAuditEntry? audit = null;
        _audits.Setup(a => a.AddAsync(It.IsAny<VerificationAuditEntry>()))
            .Callback<VerificationAuditEntry>(entry => audit = entry).Returns(Task.CompletedTask);

        // Act
        await CreateService().Handle(new VerifyProfessionalCommand(5, AdminId));

        // Assert
        Assert.Equal(VerificationStatus.Verified, professional.VerificationStatus);
        Assert.Equal(AdminId, professional.VerifiedBy);
        Assert.NotNull(professional.VerifiedAt);
        Assert.NotNull(audit);
        Assert.Equal(VerificationAuditEntry.VerifiedAction, audit!.Action);
        Assert.Equal(AdminId, audit.AdminUserId);
        Assert.Equal(5, audit.ProfessionalId);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la verificación solo procede desde PENDING; repetirla falsearía la
    ///   auditoría (otro admin y otra fecha) sobre una decisión ya tomada.
    /// - ¿Qué hace esta prueba?: intenta verificar un perfil ya VERIFIED y comprueba el 409 con su mensaje, sin
    ///   escribir auditoría ni guardar.
    /// </summary>
    [Fact]
    public async Task Verify_AlreadyVerified_ShouldThrowConflict()
    {
        // Arrange
        var professional = TestProfessionals.Verified().WithId(5);
        _professionals.Setup(p => p.FindByIdAsync(5)).ReturnsAsync(professional);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => CreateService().Handle(new VerifyProfessionalCommand(5, AdminId)));

        // Assert
        Assert.Equal("El profesional ya está verificado", exception.Message);
        _audits.Verify(a => a.AddAsync(It.IsAny<VerificationAuditEntry>()), Times.Never);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el psicólogo rechazado necesita saber por qué para corregir su solicitud;
    ///   un rechazo sin motivo (o de menos de 10 caracteres) no le sirve.
    /// - ¿Qué hace esta prueba?: rechaza sin motivo, con motivo vacío y con uno corto; comprueba el 400 en el campo
    ///   reason y que ni siquiera se busca el perfil.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Muy corto")]
    public async Task Reject_WithoutReasonOrShortReason_ShouldThrowValidation(string? reason)
    {
        // Arrange
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => service.Handle(new RejectProfessionalCommand(5, AdminId, reason)));

        // Assert
        Assert.True(exception.Errors!.ContainsKey("reason"));
        _professionals.Verify(p => p.FindByIdAsync(It.IsAny<long>()), Times.Never);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }
}
