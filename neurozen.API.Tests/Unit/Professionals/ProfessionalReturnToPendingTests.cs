using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.ValueObjects;

namespace neurozen.API.Tests.Unit.Professionals;

public class ProfessionalReturnToPendingTests
{
    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: cuando cambia la identidad cotejada por el admin, una solicitud VERIFIED o
    ///   REJECTED vuelve a PENDING; nunca se aprueba nada automáticamente.
    /// - ¿Qué hace esta prueba?: aplica la regla desde VERIFIED, REJECTED y PENDING.
    /// </summary>
    [Fact]
    public void ReturnToPending_FromVerified_ClearsVerificationData()
    {
        var professional = new Professional(1, Specialty.Clinical, "45821");
        professional.Verify(99, DateTime.UtcNow);

        var changed = professional.ReturnToPendingVerification(DateTime.UtcNow);

        Assert.True(changed);
        Assert.Equal(VerificationStatus.Pending, professional.VerificationStatus);
        Assert.Null(professional.VerifiedAt);
        Assert.Null(professional.VerifiedBy);
        Assert.False(professional.IsVisibleToPatients);
    }

    [Fact]
    public void ReturnToPending_FromRejected_ClearsReason()
    {
        var professional = new Professional(1, Specialty.Clinical, "45821");
        professional.Reject("La colegiatura no figura como habilitada", DateTime.UtcNow);

        Assert.True(professional.ReturnToPendingVerification(DateTime.UtcNow));
        Assert.Null(professional.RejectionReason);
    }

    [Fact]
    public void ReturnToPending_WhenAlreadyPending_IsNoOp()
    {
        var professional = new Professional(1, Specialty.Clinical, "45821");
        Assert.False(professional.ReturnToPendingVerification(DateTime.UtcNow));
    }

    [Theory]
    [InlineData("٤٥٨٢١")]   // dígitos arábigo-índicos: antes \d los aceptaba
    [InlineData("45 821")]
    public void LicenseNumber_NonAsciiDigitsOrSpaces_AreRejected(string license) =>
        Assert.False(Professional.IsValidLicenseNumber(license));
}
