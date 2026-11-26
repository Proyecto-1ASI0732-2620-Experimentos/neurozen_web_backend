using System.Text.Json;
using Microsoft.Extensions.Options;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Interfaces.REST.Transform;
using neurozen.API.Safety.Application.Internal.Configuration;
using neurozen.API.Safety.Application.Internal.QueryServices;
using neurozen.API.Safety.Domain.Model.Entities;
using neurozen.API.Safety.Domain.Model.Queries;
using neurozen.API.Safety.Interfaces.REST;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Tests.Unit.Support;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Safety.Interfaces.REST.Resources;

namespace neurozen.API.Tests.Unit.Safety;

/// <summary>Líneas de emergencia (solo las ya usadas por el frontend) y perfil público del psicólogo.</summary>
public class EmergencyResourceAndPublicProfileTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static EmergencyResourceQueryService Service(string? verifiedAt) =>
        new(Options.Create(new EmergencyResourcesSettings { VerifiedAt = verifiedAt }));

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: solo se publican la Línea 113 (opción 5) y el SAMU 106 de Perú; no se
    ///   inventan números ni instituciones.
    /// - ¿Qué hace esta prueba?: pide PE (por defecto y en minúsculas) y comprueba exactamente esas dos líneas.
    /// </summary>
    [Fact]
    public void Peru_ShouldListOnlyLine113AndSamu106()
    {
        // Act
        var byDefault = Service(null).Handle(new GetEmergencyResourcesQuery(null));
        var lower = Service(null).Handle(new GetEmergencyResourcesQuery(" pe "));

        // Assert
        Assert.Equal(["113", "106"], byDefault.Select(r => r.Phone));
        Assert.Equal(byDefault, lower);
        Assert.Equal("Línea 113, opción 5 (salud mental)", byDefault[0].Name);
        Assert.All(byDefault, r => Assert.Equal("PE", r.Country));
        Assert.Equal(2, EmergencyResourceCatalog.All.Count);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: otro país no tiene líneas verificadas (lista vacía) y un código mal formado
    ///   es 400.
    /// - ¿Qué hace esta prueba?: pide AR, "PER" y "P1".
    /// </summary>
    [Fact]
    public void OtherCountry_IsEmpty_InvalidCode_IsRejected()
    {
        Assert.Empty(Service(null).Handle(new GetEmergencyResourcesQuery("AR")));
        Assert.Throws<ValidationFailedException>(() => Service(null).Handle(new GetEmergencyResourcesQuery("PER")));
        Assert.Throws<ValidationFailedException>(() => Service(null).Handle(new GetEmergencyResourcesQuery("P1")));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: verifiedAt solo se informa cuando el equipo registró la fecha de
    ///   verificación (EmergencyResources:VerifiedAt); no se inventa.
    /// - ¿Qué hace esta prueba?: sin configuración → null; con fecha válida → la fecha; con texto inválido → null.
    /// </summary>
    [Fact]
    public void VerifiedAt_ComesOnlyFromConfiguration()
    {
        // Act
        var none = Ok(new EmergencyResourcesController(Service(null)).GetEmergencyResources("PE"));
        var configured = Ok(new EmergencyResourcesController(Service("2026-11-26")).GetEmergencyResources("PE"));

        // Assert
        Assert.All(none, r => Assert.Null(r.VerifiedAt));
        Assert.All(configured, r => Assert.Equal("2026-11-26", r.VerifiedAt));
        Assert.Null(Service("26/11/2026").VerifiedAt);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la tarifa no definida debe seguir siendo null (no 0), la calificación sin
    ///   reseñas es 0 con 0 reseñas, y el calendario necesita availabilitySchedule; la colegiatura no es pública.
    /// - ¿Qué hace esta prueba?: serializa el recurso público de un perfil sin tarifa y de otro con tarifa.
    /// </summary>
    [Fact]
    public void PublicProfile_KeepsNullPrice_AndExposesSchedule()
    {
        // Arrange
        var professional = TestProfessionals.Verified().WithId(5);
        var withoutPrice = TestProfessionals.Verified(21, "45822").WithId(6);
        typeof(neurozen.API.Professionals.Domain.Model.Aggregates.Professional)
            .GetProperty("Price")!.SetValue(withoutPrice, null);

        // Act
        var json = JsonSerializer.SerializeToElement(
            ProfessionalResourceFromEntityAssembler.ToResourceFromEntity(new ProfessionalDetails(professional, "Elena", "")),
            Web);
        var noPrice = JsonSerializer.SerializeToElement(
            ProfessionalResourceFromEntityAssembler.ToResourceFromEntity(new ProfessionalDetails(withoutPrice, "Luis", "")),
            Web);

        // Assert
        Assert.Equal(80m, json.GetProperty("price").GetDecimal());
        Assert.Equal(JsonValueKind.Null, noPrice.GetProperty("price").ValueKind);
        Assert.Equal(0m, noPrice.GetProperty("rating").GetDecimal());
        Assert.Equal(0, noPrice.GetProperty("reviews").GetInt32());
        var schedule = json.GetProperty("availabilitySchedule");
        Assert.Equal("09:00", schedule.GetProperty("start").GetString());
        Assert.Equal("13:00", schedule.GetProperty("end").GetString());
        Assert.Equal(7, schedule.GetProperty("days").GetArrayLength());
        Assert.False(json.TryGetProperty("licenseNumber", out _));
        Assert.Null(ProfessionalResourceFromEntityAssembler.ToScheduleResource(null));
    }

    private static List<EmergencyResourceResource> Ok(ActionResult<IEnumerable<EmergencyResourceResource>> result) =>
        Assert.IsAssignableFrom<IEnumerable<EmergencyResourceResource>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).ToList();
}
