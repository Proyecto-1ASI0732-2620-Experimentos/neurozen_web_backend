using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Entities;
using neurozen.API.Tests.Integration.Support;

namespace neurozen.API.Tests.Integration.Registration;

/// <summary>
///     Registro de usuarios y psicólogos con DNI contra la API real y MySQL: validaciones, duplicados,
///     consentimientos, protección del DNI y conservación del flujo administrativo de verificación.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class RegistrationIntegrationTests(IntegrationFixture fixture)
{
    private TestEnvironment Env => fixture.Environment;

    private static object UserBody(string email, string? dni, string? confirmPassword = TestApi.DefaultPassword,
        bool? acceptedPrivacyPolicy = true, string fullName = "Lucía Mendoza") => new
    {
        email, password = TestApi.DefaultPassword, confirmPassword, fullName, dni, role = "USER",
        acceptedTerms = true, acceptedPrivacyPolicy
    };

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el registro debe guardar el DNI y la constancia de cada consentimiento,
    ///   y el DNI no debe aparecer en respuestas que no lo necesitan (registro e inicio de sesión).
    /// - ¿Qué hace esta prueba?: registra un USER, comprueba en MySQL el DNI y los dos consentimientos con versión
    ///   y fecha, que sign-up/sign-in no contienen el DNI y que GET /users/{id} (solo el dueño) sí lo devuelve.
    /// </summary>
    [Fact]
    public async Task UserSignUp_WithDni_ShouldPersistDniAndConsents_AndNotLeakDni()
    {
        var email = TestApi.UniqueEmail("registro");
        var dni = TestApi.UniqueDni();

        var signUp = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up", UserBody(email, dni));
        await TestApi.EnsureStatusAsync(signUp, HttpStatusCode.Created);
        var body = await signUp.ReadJsonAsync();
        var userId = body.GetProperty("id").GetInt64();
        var token = body.GetProperty("token").GetString()!;
        var signIn = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { username = email, password = TestApi.DefaultPassword });
        var account = await Env.Api.WithToken(token).GetAsync($"/api/v1/users/{userId}");

        Assert.Equal("USER", body.GetProperty("role").GetString());
        Assert.DoesNotContain(dni, await signUp.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        Assert.DoesNotContain(dni, await signIn.Content.ReadAsStringAsync());
        Assert.Equal(dni, (await account.ReadJsonAsync()).GetProperty("dni").GetString());
        Assert.DoesNotContain("confirmPassword", await account.Content.ReadAsStringAsync());

        var storedDni = await Env.QueryDatabaseAsync(db =>
            db.Set<User>().Where(u => u.Id == userId).Select(u => u.Dni).SingleAsync());
        var consents = await Env.QueryDatabaseAsync(db =>
            db.Set<UserConsent>().Where(c => c.UserId == userId).ToListAsync());
        Assert.Equal(dni, storedDni);
        Assert.Equal(2, consents.Count);
        Assert.Contains(consents, c => c.Type == ConsentType.Terms && !string.IsNullOrEmpty(c.Version));
        Assert.Contains(consents, c => c.Type == ConsentType.PrivacyPolicy && !string.IsNullOrEmpty(c.Version));
        Assert.All(consents, c => Assert.True(c.AcceptedAt > DateTime.UtcNow.AddMinutes(-5)));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el DNI peruano es obligatorio y tiene exactamente 8 dígitos; el error
    ///   debe llegar al frontend en el campo dni con el cuerpo { message, errors }.
    /// - ¿Qué hace esta prueba?: envía DNI vacío, de 7 y 9 dígitos, con letras y con símbolos; todos responden 400
    ///   con errors.dni y no crean la cuenta.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("12A45678")]
    [InlineData("12.45678")]
    public async Task UserSignUp_InvalidDni_ShouldReturn400OnDniField(string? dni)
    {
        var email = TestApi.UniqueEmail("dni-invalido");

        var response = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up", UserBody(email, dni));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("dni", out _));
        Assert.False(await Env.QueryDatabaseAsync(db => db.Set<User>().AnyAsync(u => u.Email == email)));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: una persona = una cuenta; un DNI repetido debe responder 409 sin
    ///   revelar el número en el mensaje.
    /// - ¿Qué hace esta prueba?: registra dos cuentas con el mismo DNI y distinto correo; la segunda recibe 409.
    /// </summary>
    [Fact]
    public async Task UserSignUp_DuplicateDni_ShouldReturn409()
    {
        var dni = TestApi.UniqueDni();
        await TestApi.EnsureStatusAsync(await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up",
            UserBody(TestApi.UniqueEmail("dni-1"), dni)), HttpStatusCode.Created);

        var duplicate = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up",
            UserBody(TestApi.UniqueEmail("dni-2"), dni));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var message = await duplicate.MessageAsync();
        Assert.Equal("Ya existe una cuenta con este DNI", message);
        Assert.DoesNotContain(dni, await duplicate.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UserSignUp_ConfirmPasswordMismatch_ShouldReturn400()
    {
        var response = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up",
            UserBody(TestApi.UniqueEmail("confirmacion"), TestApi.UniqueDni(), confirmPassword: "OtraClave99"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("confirmPassword", out _));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: compatibilidad con el frontend actual, que envía una sola casilla
    ///   (acceptedTerms) y no envía confirmPassword; con el DNI agregado el registro debe seguir funcionando.
    /// - ¿Qué hace esta prueba?: registra con el contrato anterior + dni y comprueba 201 y los dos consentimientos.
    /// </summary>
    [Fact]
    public async Task UserSignUp_LegacyContractPlusDni_ShouldStillWork()
    {
        var response = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up", new
        {
            email = TestApi.UniqueEmail("legacy"), password = TestApi.DefaultPassword, fullName = "Pedro Salas",
            dni = TestApi.UniqueDni(), role = "USER", acceptedTerms = true
        });

        await TestApi.EnsureStatusAsync(response, HttpStatusCode.Created);
        var userId = (await response.ReadJsonAsync()).GetProperty("id").GetInt64();
        Assert.Equal(2, await Env.QueryDatabaseAsync(db => db.Set<UserConsent>().CountAsync(c => c.UserId == userId)));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el registro de psicólogos con DNI debe conservar el flujo administrativo:
    ///   queda PENDING, aparece al admin (con su DNI para la revisión), no accede a funciones profesionales, no puede
    ///   aprobarse a sí mismo ni ser aprobado por un USER, y solo tras la aprobación del admin queda habilitado.
    /// - ¿Qué hace esta prueba?: recorre todo ese ciclo contra la API real.
    /// </summary>
    [Fact]
    public async Task PsychologistSignUp_WithDni_FullAdministrativeVerificationCycle()
    {
        var psychologist = await Env.Api.AsPsychologist(verified: false, fullName: "Rocío Paredes");
        var admin = await Env.Api.AsAdmin();
        var worker = await Env.Api.AsUser();

        // Estado pendiente después del registro
        var me = await (await psychologist.Client.GetAsync("/api/v1/professionals/me")).ReadJsonAsync();
        Assert.Equal("PENDING", me.GetProperty("verificationStatus").GetString());

        // Solicitud visible para el administrador, con DNI
        var pending = await (await admin.Client.GetAsync("/api/v1/admin/professionals?status=PENDING")).ReadJsonAsync();
        var row = pending.EnumerateArray().Single(p => p.GetProperty("id").GetInt64() == psychologist.ProfessionalId);
        Assert.Matches("^[0-9]{8}$", row.GetProperty("dni").GetString()!);

        // El directorio público nunca muestra el DNI
        var directoryForWorker = await worker.Client.GetAsync("/api/v1/professionals");
        Assert.DoesNotContain("\"dni\"", await directoryForWorker.Content.ReadAsStringAsync());

        // Restricción mientras está pendiente
        Assert.Equal(HttpStatusCode.Forbidden, (await psychologist.Client.GetAsync("/api/v1/professionals")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await psychologist.Client.GetAsync(
            $"/api/v1/appointments?professionalId={psychologist.ProfessionalId}")).StatusCode);
        var booking = await TestApi.BookAsync(worker.Client, worker.UserId, psychologist.ProfessionalId,
            TestApi.SlotUtc(2, 9));
        Assert.Equal(HttpStatusCode.NotFound, booking.StatusCode);

        // Intento de autoaprobación y aprobación por un usuario sin permisos
        var selfApproval = await psychologist.Client.PatchAsync(
            $"/api/v1/admin/professionals/{psychologist.ProfessionalId}/verify", null);
        var workerApproval = await worker.Client.PatchAsync(
            $"/api/v1/admin/professionals/{psychologist.ProfessionalId}/verify", null);
        Assert.Equal(HttpStatusCode.Forbidden, selfApproval.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, workerApproval.StatusCode);
        Assert.Equal("PENDING", (await (await psychologist.Client.GetAsync("/api/v1/professionals/me"))
            .ReadJsonAsync()).GetProperty("verificationStatus").GetString());

        // Acceso habilitado después de la aprobación del administrador
        var verify = await admin.Client.PatchAsync(
            $"/api/v1/admin/professionals/{psychologist.ProfessionalId}/verify", null);
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await psychologist.Client.GetAsync("/api/v1/professionals")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await psychologist.Client.GetAsync(
            $"/api/v1/appointments?professionalId={psychologist.ProfessionalId}")).StatusCode);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: corrección de un bypass; un psicólogo VERIFIED podía cambiar su nombre
    ///   (el dato que el admin cotejó con el CPsP) mediante PUT /users/{id} y seguir verificado.
    /// - ¿Qué hace esta prueba?: cambia el nombre por PUT /users/{id} y comprueba que el perfil vuelve a PENDING.
    /// </summary>
    [Fact]
    public async Task VerifiedPsychologist_ChangingNameThroughUsersEndpoint_ShouldReturnToPending()
    {
        var psychologist = await Env.Api.AsPsychologist(verified: true, fullName: "Elena Ramírez");

        var update = await psychologist.Client.PutAsJsonAsync($"/api/v1/users/{psychologist.UserId}",
            new { fullName = "Elena Ramírez Torres" });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var me = await (await psychologist.Client.GetAsync("/api/v1/professionals/me")).ReadJsonAsync();
        Assert.Equal("PENDING", me.GetProperty("verificationStatus").GetString());
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un psicólogo que vuelve a PENDING (p. ej. cambió su colegiatura) no debe
    ///   confirmar ni leer citas con datos del paciente; antes solo se bloqueaba el listado.
    /// - ¿Qué hace esta prueba?: con una cita reservada, el psicólogo cambia su colegiatura (PENDING) e intenta
    ///   confirmar y leer la cita: 403 en ambos casos; aún puede cancelarla para liberar al paciente.
    /// </summary>
    [Fact]
    public async Task PsychologistBackToPending_CannotConfirmOrReadAppointment()
    {
        var psychologist = await Env.Api.AsPsychologist();
        var patient = await Env.Api.AsUser();
        var booking = await TestApi.BookAsync(patient.Client, patient.UserId, psychologist.ProfessionalId,
            TestApi.SlotUtc(3, 11));
        await TestApi.EnsureStatusAsync(booking, HttpStatusCode.Created);
        var appointmentId = (await booking.ReadJsonAsync()).GetProperty("id").GetInt64();
        // Outside the range used by TestApi (100000-8xxxxx) so it never collides with another test account.
        var newLicense = Random.Shared.Next(900000, 999999).ToString();
        var resubmit = await psychologist.Client.PutAsJsonAsync("/api/v1/professionals/me",
            TestApi.ProfileBody(psychologist.FullName, newLicense));
        await TestApi.EnsureStatusAsync(resubmit, HttpStatusCode.OK);

        var confirm = await psychologist.Client.PatchAsJsonAsync($"/api/v1/appointments/{appointmentId}/status",
            new { status = "CONFIRMED" });
        var read = await psychologist.Client.GetAsync($"/api/v1/appointments/{appointmentId}");
        var cancel = await psychologist.Client.PatchAsJsonAsync($"/api/v1/appointments/{appointmentId}/status",
            new { status = "CANCELLED" });

        Assert.Equal(HttpStatusCode.Forbidden, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.False((await cancel.ReadJsonAsync()).TryGetProperty("patientEmail", out var email) &&
                     email.ValueKind == System.Text.Json.JsonValueKind.String);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: al eliminar la cuenta (Ley 29733) el DNI debe liberarse y borrarse,
    ///   y los consentimientos deben conservarse como constancia.
    /// - ¿Qué hace esta prueba?: registra, elimina la cuenta y comprueba DNI nulo, consentimientos intactos y que el
    ///   mismo DNI puede registrarse de nuevo.
    /// </summary>
    [Fact]
    public async Task DeleteAccount_ShouldReleaseDniAndKeepConsents()
    {
        var dni = TestApi.UniqueDni();
        var signUp = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up",
            UserBody(TestApi.UniqueEmail("borrar"), dni));
        await TestApi.EnsureStatusAsync(signUp, HttpStatusCode.Created);
        var body = await signUp.ReadJsonAsync();
        var userId = body.GetProperty("id").GetInt64();

        var delete = await Env.Api.WithToken(body.GetProperty("token").GetString()!)
            .DeleteAsync($"/api/v1/users/{userId}");
        var again = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up",
            UserBody(TestApi.UniqueEmail("borrar-2"), dni));

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Null(await Env.QueryDatabaseAsync(db =>
            db.Set<User>().Where(u => u.Id == userId).Select(u => u.Dni).SingleAsync()));
        Assert.Equal(2, await Env.QueryDatabaseAsync(db => db.Set<UserConsent>().CountAsync(c => c.UserId == userId)));
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }
}
