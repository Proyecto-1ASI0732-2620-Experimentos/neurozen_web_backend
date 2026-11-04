using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using neurozen.API.Shared.Domain.Model.Time;

namespace neurozen.API.Tests.Integration.Support;

public record TestAccount(long UserId, string Email, string Password, string FullName, string Token, HttpClient Client);

public record TestPsychologist(
    long UserId,
    long ProfessionalId,
    string Email,
    string Password,
    string FullName,
    string LicenseNumber,
    string Token,
    HttpClient Client);

/// <summary>
///     Creates fictitious accounts through the public API and returns HTTP clients authenticated by role.
///     Every client gets its own fake IP, so rate limits never leak between tests.
/// </summary>
public sealed class TestApi(NeuroZenApiFactory factory)
{
    public const string DefaultPassword = "Secreta123";
    private static int _ipCounter;
    private static int _licenseCounter = Random.Shared.Next(100000, 800000);

    public HttpClient Anonymous(string? clientIp = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, clientIp ?? NextIp());
        return client;
    }

    public HttpClient WithToken(string token)
    {
        var client = Anonymous();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static string NextIp()
    {
        var n = Interlocked.Increment(ref _ipCounter);
        return $"10.{(n >> 16) & 255}.{(n >> 8) & 255}.{n & 255}";
    }

    public static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@neurozen.test";

    public async Task<TestAccount> AsUser(string fullName = "Ana Torres")
    {
        var email = UniqueEmail("trabajador");
        var response = await Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up", new
        {
            email, password = DefaultPassword, fullName, role = "USER", acceptedTerms = true
        });
        await EnsureStatusAsync(response, HttpStatusCode.Created);
        var body = await response.ReadJsonAsync();
        var token = body.GetProperty("token").GetString()!;
        return new TestAccount(body.GetProperty("id").GetInt64(), email, DefaultPassword, fullName, token,
            WithToken(token));
    }

    public async Task<TestPsychologist> AsPsychologist(bool verified = true, string fullName = "Elena Ramírez")
    {
        var email = UniqueEmail("psicologa");
        var license = Interlocked.Increment(ref _licenseCounter).ToString(CultureInfo.InvariantCulture);
        var signUp = await Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-up", new
        {
            email, password = DefaultPassword, fullName, role = "PSYCHOLOGIST", acceptedTerms = true,
            specialty = "clinical", licenseNumber = license
        });
        await EnsureStatusAsync(signUp, HttpStatusCode.Created);
        var signUpBody = await signUp.ReadJsonAsync();
        var token = signUpBody.GetProperty("token").GetString()!;
        var client = WithToken(token);

        var profile = await client.PutAsJsonAsync("/api/v1/professionals/me", ProfileBody(fullName, license));
        await EnsureStatusAsync(profile, HttpStatusCode.OK);
        var professionalId = (await profile.ReadJsonAsync()).GetProperty("id").GetInt64();

        if (verified)
        {
            var admin = await AsAdmin();
            var verify = await admin.Client.PatchAsync($"/api/v1/admin/professionals/{professionalId}/verify", null);
            await EnsureStatusAsync(verify, HttpStatusCode.OK);
        }

        return new TestPsychologist(signUpBody.GetProperty("id").GetInt64(), professionalId, email, DefaultPassword,
            fullName, license, token, client);
    }

    /// <summary>Whole profile for PUT /professionals/me: available every day from 00:00 to 23:59 (Lima).</summary>
    public static object ProfileBody(string fullName, string license) => new
    {
        fullName,
        specialty = "clinical",
        licenseNumber = license,
        bio = "Psicóloga clínica con experiencia en estrés laboral y ansiedad.",
        experienceYears = 8,
        price = 80,
        availabilitySchedule = new
        {
            days = new[] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" },
            start = "00:00",
            end = "23:59"
        }
    };

    public async Task<TestAccount> AsAdmin()
    {
        var token = await SignInAsync(TestEnvironment.AdminEmail, TestEnvironment.AdminPassword);
        return new TestAccount(0, TestEnvironment.AdminEmail, TestEnvironment.AdminPassword,
            "Administrador NeuroZen", token, WithToken(token));
    }

    public async Task<string> SignInAsync(string email, string password)
    {
        var response = await Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { username = email, password });
        await EnsureStatusAsync(response, HttpStatusCode.OK);
        return (await response.ReadJsonAsync()).GetProperty("token").GetString()!;
    }

    /// <summary>UTC ISO instant for a Lima date/hour (as the frontend sends it).</summary>
    public static string SlotUtc(int daysAhead, int hourLima) =>
        LimaTime.ToUtc(LimaTime.Today().AddDays(daysAhead), new TimeOnly(hourLima, 0))
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static async Task<HttpResponseMessage> BookAsync(HttpClient patientClient, long patientId,
        long professionalId, string slotUtc) =>
        await patientClient.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId, professionalId, appointmentDateTime = slotUtc, appointmentType = 1,
            notasAdicionales = "Me cuesta desconectarme del trabajo"
        });

    public static async Task EnsureStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode == expected) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} returned " +
            $"{(int)response.StatusCode} instead of {(int)expected}: {body}");
    }
}

public static class HttpResponseJsonExtensions
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "null" : text).RootElement.Clone();
    }

    public static async Task<string> MessageAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).GetProperty("message").GetString() ?? string.Empty;
}
