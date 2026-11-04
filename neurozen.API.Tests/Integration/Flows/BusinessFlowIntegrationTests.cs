using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Subscriptions.Domain.Model.Aggregates;
using neurozen.API.Tests.Integration.Support;

namespace neurozen.API.Tests.Integration.Flows;

[Collection(IntegrationCollection.Name)]
public class BusinessFlowIntegrationTests(IntegrationFixture fixture)
{
    private TestEnvironment Env => fixture.Environment;

    private static IEnumerable<long> Ids(JsonElement array) =>
        array.EnumerateArray().Select(item => item.GetProperty("id").GetInt64());

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un psicólogo recién registrado no puede aparecer ante los pacientes hasta
    ///   que el admin valide su colegiatura en el CPsP.
    /// - ¿Qué hace esta prueba?: registra un psicólogo, comprueba que no está en el directorio ni en su detalle
    ///   (404), el admin lo aprueba y entonces sí aparece con su nombre.
    /// </summary>
    [Fact]
    public async Task PsychologistLifecycle_RegisterVerifyAppearInDirectory()
    {
        // Arrange
        var psychologist = await Env.Api.AsPsychologist(verified: false, fullName: "Elena Ramírez");
        var worker = await Env.Api.AsUser();
        var admin = await Env.Api.AsAdmin();

        // Act
        var before = await (await worker.Client.GetAsync("/api/v1/professionals")).ReadJsonAsync();
        var detailBefore = await worker.Client.GetAsync($"/api/v1/professionals/{psychologist.ProfessionalId}");
        var verify = await admin.Client.PatchAsync(
            $"/api/v1/admin/professionals/{psychologist.ProfessionalId}/verify", null);
        var after = await (await worker.Client.GetAsync("/api/v1/professionals")).ReadJsonAsync();
        var detailAfter = await worker.Client.GetAsync($"/api/v1/professionals/{psychologist.ProfessionalId}");

        // Assert
        Assert.DoesNotContain(psychologist.ProfessionalId, Ids(before));
        Assert.Equal(HttpStatusCode.NotFound, detailBefore.StatusCode);
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        Assert.Contains(psychologist.ProfessionalId, Ids(after));
        Assert.Equal(HttpStatusCode.OK, detailAfter.StatusCode);
        var detail = await detailAfter.ReadJsonAsync();
        Assert.Equal("Elena Ramírez", detail.GetProperty("name").GetString());
        Assert.False(detail.TryGetProperty("licenseNumber", out _));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el psicólogo rechazado debe ver el motivo y poder reenviar su solicitud,
    ///   que vuelve a la cola del admin como PENDING.
    /// - ¿Qué hace esta prueba?: el admin rechaza con motivo, el psicólogo lo ve en /professionals/me como REJECTED,
    ///   guarda su perfil y queda PENDING sin motivo.
    /// </summary>
    [Fact]
    public async Task RejectedPsychologist_ShouldSeeReasonAndResubmitToPending()
    {
        // Arrange
        const string reason = "La colegiatura no figura como HABILITADO en el CPsP";
        var psychologist = await Env.Api.AsPsychologist(verified: false);
        var admin = await Env.Api.AsAdmin();

        // Act
        var reject = await admin.Client.PatchAsJsonAsync(
            $"/api/v1/admin/professionals/{psychologist.ProfessionalId}/reject", new { reason });
        var rejected = await (await psychologist.Client.GetAsync("/api/v1/professionals/me")).ReadJsonAsync();
        var resubmit = await psychologist.Client.PutAsJsonAsync("/api/v1/professionals/me",
            TestApi.ProfileBody(psychologist.FullName, psychologist.LicenseNumber));
        var resubmitted = await resubmit.ReadJsonAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        Assert.Equal("REJECTED", rejected.GetProperty("verificationStatus").GetString());
        Assert.Equal(reason, rejected.GetProperty("rejectionReason").GetString());
        Assert.Equal(HttpStatusCode.OK, resubmit.StatusCode);
        Assert.Equal("PENDING", resubmitted.GetProperty("verificationStatus").GetString());
        Assert.Equal(string.Empty, resubmitted.GetProperty("rejectionReason").GetString());
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: es el ciclo completo de una sesión: reservar, confirmar, atender y
    ///   calificar; al final la calificación pública del psicólogo debe reflejar la reseña.
    /// - ¿Qué hace esta prueba?: reserva (201 PENDING), el psicólogo confirma, se simula que la hora pasó (se mueve
    ///   la cita al pasado en la base), el psicólogo la marca atendida, el paciente califica con 5 y el psicólogo
    ///   pasa a rating 5.0 con 1 reseña; la cita queda reviewed.
    /// </summary>
    [Fact]
    public async Task BookingFlow_BookConfirmCompleteReview()
    {
        // Arrange
        var psychologist = await Env.Api.AsPsychologist();
        var patient = await Env.Api.AsUser();

        // Act
        var booking = await TestApi.BookAsync(patient.Client, patient.UserId, psychologist.ProfessionalId,
            TestApi.SlotUtc(1, 10));
        var appointmentId = (await booking.ReadJsonAsync()).GetProperty("id").GetInt64();
        var confirm = await psychologist.Client.PatchAsJsonAsync($"/api/v1/appointments/{appointmentId}/status",
            new { status = "CONFIRMED" });
        await Env.WithDatabaseAsync(db => db.Set<Appointment>().Where(a => a.Id == appointmentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.AppointmentDateTime, DateTime.UtcNow.AddHours(-2))
                .SetProperty(a => a.ActiveSlot, DateTime.UtcNow.AddHours(-2))));
        var complete = await psychologist.Client.PatchAsJsonAsync($"/api/v1/appointments/{appointmentId}/status",
            new { status = "COMPLETED" });
        var review = await patient.Client.PostAsJsonAsync(
            $"/api/v1/professionals/{psychologist.ProfessionalId}/reviews",
            new { appointmentId, rating = 5, comment = "Muy útil" });
        var profile = await (await patient.Client.GetAsync($"/api/v1/professionals/{psychologist.ProfessionalId}"))
            .ReadJsonAsync();

        // Assert
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
        Assert.Equal("CONFIRMED", (await confirm.ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal("COMPLETED", (await complete.ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Created, review.StatusCode);
        Assert.Equal("Ana T.", (await review.ReadJsonAsync()).GetProperty("userName").GetString());
        Assert.Equal(5.0m, profile.GetProperty("rating").GetDecimal());
        Assert.Equal(1, profile.GetProperty("reviews").GetInt32());
        var reviewed = await Env.QueryDatabaseAsync(db =>
            db.Set<Appointment>().Where(a => a.Id == appointmentId).Select(a => a.Reviewed).SingleAsync());
        Assert.True(reviewed);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: doble reserva contra la base real; el segundo paciente que pide la misma
    ///   hora con el mismo psicólogo debe recibir 409 y en la base debe quedar una sola cita activa.
    /// - ¿Qué hace esta prueba?: dos pacientes reservan el mismo horario; el primero 201, el segundo 409 con su
    ///   mensaje, y se cuenta una sola cita PENDING para ese horario.
    /// </summary>
    [Fact]
    public async Task Booking_SameSlotTwice_SecondShouldReturn409()
    {
        // Arrange
        var psychologist = await Env.Api.AsPsychologist();
        var ana = await Env.Api.AsUser();
        var luis = await Env.Api.AsUser("Luis Paredes");
        var slot = TestApi.SlotUtc(2, 15);

        // Act
        var first = await TestApi.BookAsync(ana.Client, ana.UserId, psychologist.ProfessionalId, slot);
        var second = await TestApi.BookAsync(luis.Client, luis.UserId, psychologist.ProfessionalId, slot);

        // Assert
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("El horario ya no está disponible", await second.MessageAsync());
        var active = await Env.QueryDatabaseAsync(db => db.Set<Appointment>().CountAsync(a =>
            a.ProfessionalId == psychologist.ProfessionalId && a.Status == AppointmentStatus.Pending));
        Assert.Equal(1, active);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un informe compartido es información de salud; solo lo ve el psicólogo de
    ///   la cita, nunca otro psicólogo.
    /// - ¿Qué hace esta prueba?: el paciente comparte un informe en su cita con el psicólogo A; A lo ve, B recibe una
    ///   lista vacía con su propio id y 403 si pide los informes de A.
    /// </summary>
    [Fact]
    public async Task SharedReport_VisibleToItsPsychologistOnly()
    {
        // Arrange
        var psychologistA = await Env.Api.AsPsychologist();
        var psychologistB = await Env.Api.AsPsychologist(fullName: "Jorge Salas");
        var patient = await Env.Api.AsUser();
        var booking = await TestApi.BookAsync(patient.Client, patient.UserId, psychologistA.ProfessionalId,
            TestApi.SlotUtc(1, 11));
        var appointmentId = (await booking.ReadJsonAsync()).GetProperty("id").GetInt64();

        // Act
        var share = await patient.Client.PostAsJsonAsync("/api/v1/reports", new
        {
            patientId = patient.UserId, professionalId = psychologistA.ProfessionalId, appointmentId,
            stressCurrent = 70, stressAverage = 62, weeklyChange = -5, peakHours = "15:00 - 17:00",
            triggersCount = 4, topCategory = "meeting",
            lastAssessment = new { score = 22, level = "moderate", completedAt = DateTime.UtcNow.AddDays(-1) },
            note = "Semana de cierre muy pesada"
        });
        var seenByA = await (await psychologistA.Client.GetAsync(
            $"/api/v1/reports?professionalId={psychologistA.ProfessionalId}")).ReadJsonAsync();
        var seenByBOwn = await (await psychologistB.Client.GetAsync(
            $"/api/v1/reports?professionalId={psychologistB.ProfessionalId}")).ReadJsonAsync();
        var bAskingForA = await psychologistB.Client.GetAsync(
            $"/api/v1/reports?professionalId={psychologistA.ProfessionalId}");

        // Assert
        Assert.Equal(HttpStatusCode.Created, share.StatusCode);
        var reportId = (await share.ReadJsonAsync()).GetProperty("id").GetInt64();
        Assert.Contains(reportId, Ids(seenByA));
        Assert.DoesNotContain(reportId, Ids(seenByBOwn));
        Assert.Equal(HttpStatusCode.Forbidden, bAskingForA.StatusCode);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el usuario que olvidó su contraseña debe recuperarla con el enlace del
    ///   correo; después, la contraseña anterior deja de servir.
    /// - ¿Qué hace esta prueba?: pide la recuperación, toma el token del correo capturado, restablece, y comprueba
    ///   que la contraseña nueva inicia sesión (200) y la anterior no (401).
    /// </summary>
    [Fact]
    public async Task PasswordRecovery_ForgotResetSignInWithNewPassword()
    {
        // Arrange
        var user = await Env.Api.AsUser();
        const string newPassword = "NuevaClave2026";

        // Act
        var forgot = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/forgot-password",
            new { email = user.Email });
        var email = Env.Factory.Emails.LastSentTo(user.Email);
        var token = Uri.UnescapeDataString(Regex.Match(email!.TextBody, @"token=([^\s]+)").Groups[1].Value);
        var reset = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/reset-password",
            new { token, newPassword });
        var withNew = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { username = user.Email, password = newPassword });
        var withOld = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { username = user.Email, password = user.Password });

        // Assert
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: derecho de cancelación (Ley 29733); al eliminar la cuenta se borran sus
    ///   datos de salud y el acceso queda invalidado de inmediato, incluso con un token aún vigente.
    /// - ¿Qué hace esta prueba?: el usuario registra una situación de estrés, elimina su cuenta (204) y se comprueba
    ///   que su token da 401, que ya no puede iniciar sesión, que sus registros no existen y que la fila quedó
    ///   anonimizada.
    /// </summary>
    [Fact]
    public async Task DeleteAccount_ShouldRemoveDataAndInvalidateAccess()
    {
        // Arrange
        var user = await Env.Api.AsUser();
        var trigger = await user.Client.PostAsJsonAsync("/api/v1/triggers", new
        {
            userId = user.UserId, description = "Reunión con el gerente", category = "meeting", stressLevel = 8,
            triggeredAt = DateTime.UtcNow.AddHours(-1)
        });
        await TestApi.EnsureStatusAsync(trigger, HttpStatusCode.Created);

        // Act
        var delete = await user.Client.DeleteAsync($"/api/v1/users/{user.UserId}");
        var withOldToken = await user.Client.GetAsync($"/api/v1/users/{user.UserId}");
        var signIn = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { username = user.Email, password = user.Password });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, withOldToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, signIn.StatusCode);
        var triggers = await Env.QueryDatabaseAsync(db => db.Set<StressTrigger>().CountAsync(t => t.UserId == user.UserId));
        Assert.Equal(0, triggers);
        var stored = await Env.QueryDatabaseAsync(db => db.Set<User>().SingleAsync(u => u.Id == user.UserId));
        Assert.NotNull(stored.DeletedAt);
        Assert.NotEqual(user.Email, stored.Email);
        Assert.Equal(string.Empty, stored.PasswordHash);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: PCI DSS de punta a punta; tras suscribirse, ni la respuesta ni la base
    ///   tienen el número completo ni el CVV, solo los últimos 4 dígitos.
    /// - ¿Qué hace esta prueba?: crea la suscripción (201) y la consulta (200 con un objeto); ambas respuestas tienen
    ///   cardLast4 1111 sin cvv ni numberCard, y la fila guardada tiene solo 1111 y el vencimiento.
    /// </summary>
    [Fact]
    public async Task Subscription_CreateAndGet_ShouldOnlyExposeLast4()
    {
        // Arrange
        var user = await Env.Api.AsUser();

        // Act
        var create = await user.Client.PostAsJsonAsync("/api/v1/subscriptions", new
        {
            userId = user.UserId, planId = 2, nameUser = "Ana", lastNameUser = "Torres", emailUser = user.Email,
            numberCard = "4111111111111111", expirationDate = "12/30", cvv = "123", isActive = false
        });
        var get = await user.Client.GetAsync($"/api/v1/subscriptions/user/{user.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        foreach (var body in new[] { await create.ReadJsonAsync(), await get.ReadJsonAsync() })
        {
            Assert.Equal(JsonValueKind.Object, body.ValueKind);
            Assert.Equal("1111", body.GetProperty("cardLast4").GetString());
            Assert.False(body.TryGetProperty("cvv", out _));
            Assert.False(body.TryGetProperty("numberCard", out _));
        }

        var stored = await Env.QueryDatabaseAsync(db =>
            db.Set<Subscription>().SingleAsync(s => s.UserId == user.UserId));
        Assert.Equal("1111", stored.CardLast4);
        Assert.Equal("12/30", stored.CardExpiry);
    }
}
