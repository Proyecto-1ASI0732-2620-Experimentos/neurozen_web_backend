using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using neurozen.API.Shared.Domain.Model.Time;
using neurozen.API.Tests.Integration.Support;

namespace neurozen.API.Tests.Integration.Flows;

/// <summary>
///     Contrato que espera el frontend web mejorado (B1-B9): horas libres con duración, disponibilidad por rango,
///     409 en reservas, búsqueda de recursos por plan, líneas de emergencia públicas, PSS-10 recalculado y errores
///     siempre con "message" legible. API real + MySQL 8.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class FrontendContractIntegrationTests(IntegrationFixture fixture)
{
    private TestEnvironment Env => fixture.Environment;

    private static string Date(int daysAhead) =>
        LimaTime.Today().AddDays(daysAhead).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string SlotUtc(int daysAhead, int hour, int minute) =>
        LimaTime.ToUtc(LimaTime.Today().AddDays(daysAhead), new TimeOnly(hour, minute))
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static async Task<string[]> SlotsAsync(HttpClient client, long professionalId, string query)
    {
        var response = await client.GetAsync($"/api/v1/professionals/{professionalId}/available-slots?{query}");
        await TestApi.EnsureStatusAsync(response, HttpStatusCode.OK);
        return (await response.ReadJsonAsync()).EnumerateArray().Select(e => e.GetString()!).ToArray();
    }

    private static async Task AssertReadableErrorAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        var body = await response.ReadJsonAsync();
        var message = body.GetProperty("message");
        Assert.Equal(JsonValueKind.String, message.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(message.GetString()));
        Assert.NotEqual("null", message.GetString(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el perfil público debe traer availabilitySchedule para el calendario, sin
    ///   colegiatura, y las horas libres deben respetar la duración de cada cita.
    /// - ¿Qué hace esta prueba?: reserva un seguimiento (30 min) a las 10:30 y comprueba que el bloque de 60 min de
    ///   las 10:00 desaparece, que el de 30 min de las 10:30 también, y que reservar una terapia de 60 min a las 10:00
    ///   da 409 por solapamiento.
    /// </summary>
    [Fact]
    public async Task Slots_AndBooking_ShouldRespectAppointmentDurations()
    {
        // Arrange
        var psychologist = await Env.Api.AsPsychologist();
        var patient = await Env.Api.AsUser();
        var other = await Env.Api.AsUser("Luis Paredes");

        // Act
        var profile = await (await patient.Client.GetAsync($"/api/v1/professionals/{psychologist.ProfessionalId}"))
            .ReadJsonAsync();
        var followUp = await patient.Client.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId = patient.UserId, professionalId = psychologist.ProfessionalId,
            appointmentDateTime = SlotUtc(2, 10, 30), appointmentType = 3, notasAdicionales = "seguimiento"
        });
        var hourly = await SlotsAsync(patient.Client, psychologist.ProfessionalId, $"date={Date(2)}");
        var halfHour = await SlotsAsync(patient.Client, psychologist.ProfessionalId,
            $"date={Date(2)}&durationMinutes=30");
        var overlapping = await TestApi.BookAsync(other.Client, other.UserId, psychologist.ProfessionalId,
            SlotUtc(2, 10, 0));

        // Assert
        var schedule = profile.GetProperty("availabilitySchedule");
        Assert.Equal("00:00", schedule.GetProperty("start").GetString());
        Assert.Equal(7, schedule.GetProperty("days").GetArrayLength());
        Assert.False(profile.TryGetProperty("licenseNumber", out _));
        await TestApi.EnsureStatusAsync(followUp, HttpStatusCode.Created);
        Assert.True((await followUp.ReadJsonAsync()).GetProperty("id").GetInt64() > 0);
        Assert.Contains("09:00", hourly);
        Assert.DoesNotContain("10:00", hourly);
        Assert.Contains("11:00", hourly);
        Assert.Contains("10:00", halfHour);
        Assert.DoesNotContain("10:30", halfHour);
        Assert.Contains("11:00", halfHour);
        await AssertReadableErrorAsync(overlapping, HttpStatusCode.Conflict);
        Assert.Equal("El horario ya no está disponible", await overlapping.MessageAsync());
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: una hora fuera del horario del profesional es 409 (el frontend pide otra
    ///   hora sin perder datos); una fecha pasada es 400; una duración inválida es 400.
    /// - ¿Qué hace esta prueba?: psicólogo que atiende solo hasta las 12:00; reserva a las 15:00, en el pasado y pide
    ///   horas con durationMinutes=5.
    /// </summary>
    [Fact]
    public async Task Booking_OutsideScheduleIs409_PastDateIs400()
    {
        // Arrange
        var psychologist = await Env.Api.AsPsychologist();
        var morningOnly = await psychologist.Client.PutAsJsonAsync("/api/v1/professionals/me", new
        {
            fullName = psychologist.FullName, specialty = "clinical", licenseNumber = psychologist.LicenseNumber,
            bio = "Psicóloga clínica con experiencia en estrés laboral y ansiedad.", experienceYears = 8, price = 80,
            availabilitySchedule = new
            {
                days = new[] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" },
                start = "08:00", end = "12:00"
            }
        });
        await TestApi.EnsureStatusAsync(morningOnly, HttpStatusCode.OK);
        var patient = await Env.Api.AsUser();

        // Act
        var outside = await TestApi.BookAsync(patient.Client, patient.UserId, psychologist.ProfessionalId,
            SlotUtc(2, 15, 0));
        var past = await TestApi.BookAsync(patient.Client, patient.UserId, psychologist.ProfessionalId,
            SlotUtc(-1, 9, 0));
        var badDuration = await patient.Client.GetAsync(
            $"/api/v1/professionals/{psychologist.ProfessionalId}/available-slots?date={Date(2)}&durationMinutes=5");

        // Assert
        await AssertReadableErrorAsync(outside, HttpStatusCode.Conflict);
        Assert.Equal("El horario elegido no está dentro de la disponibilidad del profesional",
            await outside.MessageAsync());
        await AssertReadableErrorAsync(past, HttpStatusCode.BadRequest);
        await AssertReadableErrorAsync(badDuration, HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: GET /appointments/availability alimenta el calendario mensual: todos los días
    ///   del rango, mismas reglas que available-slots, autorización por rol y límites.
    /// - ¿Qué hace esta prueba?: un paciente pide 3 días; se comprueban el formato, una hora ocupada que no aparece,
    ///   nextAvailableDate; el psicólogo ve su propio calendario pero no el de otro; rango de 93 días → 400; un
    ///   profesional no verificado → 404; sin sesión → 401.
    /// </summary>
    [Fact]
    public async Task AvailabilityRange_ContractAuthorizationAndLimits()
    {
        // Arrange
        var psychologist = await Env.Api.AsPsychologist();
        var otherPsychologist = await Env.Api.AsPsychologist(fullName: "Rosa Quispe");
        var pending = await Env.Api.AsPsychologist(verified: false, fullName: "Mario Díaz");
        var patient = await Env.Api.AsUser();
        var admin = await Env.Api.AsAdmin();
        await TestApi.EnsureStatusAsync(await TestApi.BookAsync(patient.Client, patient.UserId,
            psychologist.ProfessionalId, SlotUtc(2, 9, 0)), HttpStatusCode.Created);
        string Url(long id, int from, int to) =>
            $"/api/v1/appointments/availability?professionalId={id}&startDate={Date(from)}&endDate={Date(to)}";

        // Act
        var response = await patient.Client.GetAsync(Url(psychologist.ProfessionalId, 1, 3));
        var own = await psychologist.Client.GetAsync(Url(psychologist.ProfessionalId, 1, 1));
        var foreign = await psychologist.Client.GetAsync(Url(otherPsychologist.ProfessionalId, 1, 1));
        var byAdmin = await admin.Client.GetAsync(Url(psychologist.ProfessionalId, 1, 1));
        var tooLong = await patient.Client.GetAsync(Url(psychologist.ProfessionalId, 0, 92));
        var notVerified = await patient.Client.GetAsync(Url(pending.ProfessionalId, 1, 1));
        var anonymous = await Env.Api.Anonymous().GetAsync(Url(psychologist.ProfessionalId, 1, 1));

        // Assert
        await TestApi.EnsureStatusAsync(response, HttpStatusCode.OK);
        var body = await response.ReadJsonAsync();
        Assert.Equal(psychologist.ProfessionalId, body.GetProperty("professionalId").GetInt64());
        Assert.Equal("America/Lima", body.GetProperty("timeZone").GetString());
        Assert.Equal(60, body.GetProperty("slotMinutes").GetInt32());
        var days = body.GetProperty("days").EnumerateArray().ToList();
        Assert.Equal([Date(1), Date(2), Date(3)], days.Select(d => d.GetProperty("date").GetString()));
        var dayTwo = days[1].GetProperty("slots").EnumerateArray().Select(s => s.GetString()).ToList();
        Assert.DoesNotContain("09:00", dayTwo);
        Assert.Contains("10:00", dayTwo);
        Assert.Equal(Date(1), body.GetProperty("nextAvailableDate").GetString());
        await TestApi.EnsureStatusAsync(own, HttpStatusCode.OK);
        await TestApi.EnsureStatusAsync(byAdmin, HttpStatusCode.OK);
        await AssertReadableErrorAsync(foreign, HttpStatusCode.Forbidden);
        await AssertReadableErrorAsync(tooLong, HttpStatusCode.BadRequest);
        await AssertReadableErrorAsync(notVerified, HttpStatusCode.NotFound);
        await AssertReadableErrorAsync(anonymous, HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: ZenBot solo puede sugerir recursos que el plan del usuario incluye, en orden
    ///   de relevancia.
    /// - ¿Qué hace esta prueba?: busca «respiración» con un usuario sin plan; todos los resultados están en su listado
    ///   de la biblioteca, vienen ordenados por score e incluyen durationMinutes; sin q ni tags → 400.
    /// </summary>
    [Fact]
    public async Task ResourceSearch_RespectsPlanAndRelevance()
    {
        // Arrange
        var basic = await Env.Api.AsUser();
        var visible = (await (await basic.Client.GetAsync("/api/v1/resource-libraries")).ReadJsonAsync())
            .EnumerateArray().Select(r => r.GetProperty("id").GetInt64()).ToHashSet();

        // Act
        var response = await basic.Client.GetAsync("/api/v1/resource-libraries/search?q=RESPIRACION&limit=10");
        var invalid = await basic.Client.GetAsync("/api/v1/resource-libraries/search");

        // Assert
        await TestApi.EnsureStatusAsync(response, HttpStatusCode.OK);
        var items = (await response.ReadJsonAsync()).GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, item => Assert.Contains(item.GetProperty("id").GetInt64(), visible));
        Assert.All(items, item => Assert.True(item.GetProperty("durationMinutes").GetInt32() >= 0));
        var scores = items.Select(i => i.GetProperty("score").GetDouble()).ToList();
        Assert.Equal(scores.OrderByDescending(s => s), scores);
        Assert.Contains(items, i => i.GetProperty("title").GetString() == "Técnicas de Respiración Profunda");
        await AssertReadableErrorAsync(invalid, HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: las líneas de crisis deben poder consultarse incluso sin sesión.
    /// - ¿Qué hace esta prueba?: pide /emergency-resources?country=PE sin token y comprueba la Línea 113 y el SAMU 106.
    /// </summary>
    [Fact]
    public async Task EmergencyResources_ArePublic()
    {
        // Act
        var response = await Env.Api.Anonymous().GetAsync("/api/v1/emergency-resources?country=PE");

        // Assert
        await TestApi.EnsureStatusAsync(response, HttpStatusCode.OK);
        var phones = (await response.ReadJsonAsync()).EnumerateArray()
            .Select(e => e.GetProperty("phone").GetString()).ToList();
        Assert.Equal(["113", "106"], phones);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el backend recalcula el PSS-10 (ítems 4, 5, 7 y 8 invertidos) y no confía en
    ///   score/level del cliente; solo el propio usuario lee sus resultados.
    /// - ¿Qué hace esta prueba?: envía todas las respuestas en 4 con score=0 y level="low": el servidor guarda 24
    ///   (moderate); otro usuario recibe 403 al pedir esos resultados.
    /// </summary>
    [Fact]
    public async Task Pss10_IsRecalculatedOnServer_AndPrivate()
    {
        // Arrange
        var user = await Env.Api.AsUser();
        var other = await Env.Api.AsUser("Luis Paredes");

        // Act
        var created = await user.Client.PostAsJsonAsync("/api/v1/assessments", new
        {
            userId = user.UserId, answers = Enumerable.Repeat(4, 10).ToArray(), score = 0, level = "low",
            completedAt = DateTime.UtcNow
        });
        var foreign = await other.Client.GetAsync($"/api/v1/assessments?userId={user.UserId}");

        // Assert
        await TestApi.EnsureStatusAsync(created, HttpStatusCode.Created);
        var body = await created.ReadJsonAsync();
        Assert.Equal(24, body.GetProperty("score").GetInt32());
        Assert.Equal("moderate", body.GetProperty("level").GetString());
        await AssertReadableErrorAsync(foreign, HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: en el login apareció «null»; toda respuesta de error debe traer un message
    ///   de texto legible, también las que antes salían sin cuerpo o como ProblemDetails.
    /// - ¿Qué hace esta prueba?: credenciales inválidas (401), cuerpo vacío (400), Content-Type no JSON (415), ruta
    ///   inexistente (404) y método no permitido (405).
    /// </summary>
    [Fact]
    public async Task Errors_AlwaysCarryAReadableMessage()
    {
        // Arrange
        var anonymous = Env.Api.Anonymous();
        var signedIn = (await Env.Api.AsUser()).Client;

        // Act
        var wrongPassword = await anonymous.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { username = "nadie@neurozen.test", password = "Incorrecta123" });
        var emptyBody = await anonymous.PostAsync("/api/v1/authentication/sign-in",
            new StringContent("", Encoding.UTF8, "application/json"));
        var plainText = await anonymous.PostAsync("/api/v1/authentication/sign-in",
            new StringContent("hola", Encoding.UTF8, "text/plain"));
        var unknownRoute = await signedIn.GetAsync("/api/v1/no-existe");
        var wrongMethod = await signedIn.DeleteAsync("/api/v1/authentication/sign-in");

        // Assert
        await AssertReadableErrorAsync(wrongPassword, HttpStatusCode.Unauthorized);
        Assert.Equal("Correo o contraseña incorrectos", await wrongPassword.MessageAsync());
        await AssertReadableErrorAsync(emptyBody, HttpStatusCode.BadRequest);
        await AssertReadableErrorAsync(plainText, HttpStatusCode.UnsupportedMediaType);
        await AssertReadableErrorAsync(unknownRoute, HttpStatusCode.NotFound);
        await AssertReadableErrorAsync(wrongMethod, HttpStatusCode.MethodNotAllowed);
    }
}
