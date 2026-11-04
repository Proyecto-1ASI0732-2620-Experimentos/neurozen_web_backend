using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Tests.BDD.Support;
using neurozen.API.Tests.Integration.Support;
using Reqnroll;

namespace neurozen.API.Tests.BDD.Steps;

[Binding]
public sealed class NeuroZenSteps(ScenarioState state)
{
    private static Task<TestEnvironment> Env() => TestEnvironment.GetAsync();

    private TestAccount Worker => state.Worker ?? throw new InvalidOperationException("No hay trabajador");
    private TestPsychologist Psychologist => state.Psychologist ?? throw new InvalidOperationException("No hay psicólogo");
    private HttpResponseMessage LastResponse => state.LastResponse ?? throw new InvalidOperationException("Sin respuesta");

    // ------------------------------------------------------------------ Dado

    [Given("un psicólogo registrado pendiente de verificación")]
    [Given("un psicólogo pendiente de verificación con disponibilidad todos los días")]
    public async Task GivenPendingPsychologist() =>
        state.Psychologist = await (await Env()).Api.AsPsychologist(verified: false);

    [Given("un psicólogo verificado con disponibilidad todos los días")]
    public async Task GivenVerifiedPsychologist() =>
        state.Psychologist = await (await Env()).Api.AsPsychologist(verified: true);

    [Given("un trabajador con sesión iniciada")]
    [Given("un trabajador registrado que olvidó su contraseña")]
    public async Task GivenWorker() => state.Worker = await (await Env()).Api.AsUser();

    [Given("otro trabajador ya reservó la sesión de mañana a las 10:00")]
    public async Task GivenOtherWorkerBooked()
    {
        state.OtherWorker = await (await Env()).Api.AsUser("Luis Paredes");
        var response = await TestApi.BookAsync(state.OtherWorker.Client, state.OtherWorker.UserId,
            Psychologist.ProfessionalId, TestApi.SlotUtc(1, 10));
        await TestApi.EnsureStatusAsync(response, HttpStatusCode.Created);
    }

    [Given("un trabajador con una cita pendiente con un psicólogo verificado")]
    public async Task GivenPendingAppointment()
    {
        var env = await Env();
        state.Psychologist = await env.Api.AsPsychologist(verified: true);
        state.Worker = await env.Api.AsUser();
        var response = await TestApi.BookAsync(Worker.Client, Worker.UserId, Psychologist.ProfessionalId,
            TestApi.SlotUtc(1, 10));
        await TestApi.EnsureStatusAsync(response, HttpStatusCode.Created);
        state.AppointmentId = (await response.ReadJsonAsync()).GetProperty("id").GetInt64();
    }

    [Given("que el psicólogo confirmó la cita")]
    public async Task GivenConfirmed() =>
        await TestApi.EnsureStatusAsync(await ChangeStatus(Psychologist.Client, "CONFIRMED"), HttpStatusCode.OK);

    [Given("la sesión ya ocurrió")]
    public async Task GivenSessionHappened() =>
        await (await Env()).WithDatabaseAsync(db => db.Set<Appointment>().Where(a => a.Id == state.AppointmentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.AppointmentDateTime, DateTime.UtcNow.AddHours(-2))
                .SetProperty(a => a.ActiveSlot, DateTime.UtcNow.AddHours(-2))));

    [Given("el psicólogo marcó la cita como atendida")]
    public async Task GivenCompleted() =>
        await TestApi.EnsureStatusAsync(await ChangeStatus(Psychologist.Client, "COMPLETED"), HttpStatusCode.OK);

    // ------------------------------------------------------------------ Cuando

    [When("el administrador lo aprueba tras validar su colegiatura en el CPsP")]
    public async Task WhenAdminApproves()
    {
        var admin = await (await Env()).Api.AsAdmin();
        state.LastResponse = await admin.Client.PatchAsync(
            $"/api/v1/admin/professionals/{Psychologist.ProfessionalId}/verify", null);
    }

    [When("el administrador lo rechaza con el motivo {string}")]
    public async Task WhenAdminRejects(string reason)
    {
        var admin = await (await Env()).Api.AsAdmin();
        state.LastResponse = await admin.Client.PatchAsJsonAsync(
            $"/api/v1/admin/professionals/{Psychologist.ProfessionalId}/reject", new { reason });
    }

    [When("un trabajador consulta el directorio de profesionales")]
    public async Task WhenWorkerReadsDirectory()
    {
        state.Worker = await (await Env()).Api.AsUser();
        state.LastResponse = await Worker.Client.GetAsync("/api/v1/professionals");
    }

    [When("el trabajador reserva la sesión de mañana a las 10:00")]
    public async Task WhenWorkerBooks()
    {
        state.LastResponse = await TestApi.BookAsync(Worker.Client, Worker.UserId, Psychologist.ProfessionalId,
            TestApi.SlotUtc(1, 10));
        if (state.LastResponse.StatusCode == HttpStatusCode.Created)
            state.AppointmentId = (await state.LastResponse.ReadJsonAsync()).GetProperty("id").GetInt64();
    }

    [When("el psicólogo confirma la cita")]
    public async Task WhenPsychologistConfirms() => state.LastResponse = await ChangeStatus(Psychologist.Client, "CONFIRMED");

    [When("el trabajador cancela la cita")]
    public async Task WhenWorkerCancels() => state.LastResponse = await ChangeStatus(Worker.Client, "CANCELLED");

    [When("el trabajador intenta marcar la cita como atendida")]
    public async Task WhenWorkerCompletes() => state.LastResponse = await ChangeStatus(Worker.Client, "COMPLETED");

    [When("el trabajador califica la sesión con {int} estrellas")]
    public async Task WhenWorkerReviews(int rating) =>
        state.LastResponse = await Worker.Client.PostAsJsonAsync(
            $"/api/v1/professionals/{Psychologist.ProfessionalId}/reviews",
            new { appointmentId = state.AppointmentId, rating, comment = "Me ayudó mucho" });

    [When("solicita recuperar su contraseña")]
    public async Task WhenForgotPassword()
    {
        var env = await Env();
        var response = await env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/forgot-password",
            new { email = Worker.Email });
        await TestApi.EnsureStatusAsync(response, HttpStatusCode.OK);
        var email = env.Factory.Emails.LastSentTo(Worker.Email)
                    ?? throw new InvalidOperationException("No se envió el correo de recuperación");
        state.ResetToken = Uri.UnescapeDataString(Regex.Match(email.TextBody, @"token=([^\s]+)").Groups[1].Value);
    }

    [When("el enlace recibido vence")]
    public async Task WhenLinkExpires() =>
        await (await Env()).WithDatabaseAsync(db => db.Set<PasswordResetToken>()
            .Where(t => t.UserId == Worker.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTime.UtcNow.AddMinutes(-1))));

    [When("usa el enlace recibido para elegir la contraseña {string}")]
    public async Task WhenResetPassword(string newPassword) =>
        state.LastResponse = await (await Env()).Api.Anonymous().PostAsJsonAsync(
            "/api/v1/authentication/reset-password", new { token = state.ResetToken, newPassword });

    [When("cambia su contraseña indicando {string} como contraseña actual")]
    public async Task WhenChangePassword(string currentPassword) =>
        state.LastResponse = await Worker.Client.PutAsJsonAsync($"/api/v1/users/{Worker.UserId}/password",
            new { currentPassword, newPassword = "OtraClave2026" });

    // ------------------------------------------------------------------ Entonces

    [Then("el estado del psicólogo es {string}")]
    public async Task ThenPsychologistStatus(string expected)
    {
        var profile = await (await Psychologist.Client.GetAsync("/api/v1/professionals/me")).ReadJsonAsync();
        Assert.Equal(expected, profile.GetProperty("verificationStatus").GetString());
    }

    [Then("el psicólogo ve el motivo {string}")]
    public async Task ThenPsychologistSeesReason(string reason)
    {
        var profile = await (await Psychologist.Client.GetAsync("/api/v1/professionals/me")).ReadJsonAsync();
        Assert.Equal(reason, profile.GetProperty("rejectionReason").GetString());
    }

    [Then("el psicólogo aparece en el directorio de profesionales")]
    public async Task ThenPsychologistInDirectory()
    {
        var worker = state.Worker ?? await (await Env()).Api.AsUser();
        var directory = await (await worker.Client.GetAsync("/api/v1/professionals")).ReadJsonAsync();
        Assert.Contains(Psychologist.ProfessionalId,
            directory.EnumerateArray().Select(p => p.GetProperty("id").GetInt64()));
    }

    [Then("el psicólogo no aparece en el directorio de profesionales")]
    public async Task ThenPsychologistNotInDirectory()
    {
        Assert.Equal(HttpStatusCode.OK, LastResponse.StatusCode);
        var directory = await LastResponse.ReadJsonAsync();
        Assert.DoesNotContain(Psychologist.ProfessionalId,
            directory.EnumerateArray().Select(p => p.GetProperty("id").GetInt64()));
    }

    [Then("la respuesta es {int}")]
    public void ThenStatus(int status) => Assert.Equal(status, (int)LastResponse.StatusCode);

    [Then("la respuesta es un error {int}")]
    public async Task ThenError(int status)
    {
        Assert.Equal(status, (int)LastResponse.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await LastResponse.MessageAsync()));
    }

    [Then("la respuesta es un error {int} con el mensaje {string}")]
    public async Task ThenErrorWithMessage(int status, string message)
    {
        Assert.Equal(status, (int)LastResponse.StatusCode);
        Assert.Equal(message, await LastResponse.MessageAsync());
    }

    [Then("la cita queda en estado {string}")]
    public async Task ThenAppointmentStatus(string expected)
    {
        var appointment = await (await Worker.Client.GetAsync($"/api/v1/appointments/{state.AppointmentId}"))
            .ReadJsonAsync();
        Assert.Equal(expected, appointment.GetProperty("status").GetString());
    }

    [Then("la calificación del psicólogo es {string} con {int} reseña")]
    public async Task ThenRating(string rating, int reviews)
    {
        var profile = await (await Worker.Client.GetAsync($"/api/v1/professionals/{Psychologist.ProfessionalId}"))
            .ReadJsonAsync();
        Assert.Equal(decimal.Parse(rating, CultureInfo.InvariantCulture), profile.GetProperty("rating").GetDecimal());
        Assert.Equal(reviews, profile.GetProperty("reviews").GetInt32());
    }

    [Then("puede iniciar sesión con la contraseña {string}")]
    public async Task ThenCanSignInWith(string password) =>
        Assert.Equal(HttpStatusCode.OK, await SignInStatus(password));

    [Then("ya no puede iniciar sesión con su contraseña anterior")]
    public async Task ThenCannotSignInWithOld() =>
        Assert.Equal(HttpStatusCode.Unauthorized, await SignInStatus(Worker.Password));

    [Then("puede iniciar sesión con su contraseña anterior")]
    public async Task ThenCanSignInWithOld() => Assert.Equal(HttpStatusCode.OK, await SignInStatus(Worker.Password));

    // ------------------------------------------------------------------ helpers

    private async Task<HttpResponseMessage> ChangeStatus(HttpClient client, string status) =>
        await client.PatchAsJsonAsync($"/api/v1/appointments/{state.AppointmentId}/status", new { status });

    private async Task<HttpStatusCode> SignInStatus(string password) =>
        (await (await Env()).Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { username = Worker.Email, password })).StatusCode;
}
