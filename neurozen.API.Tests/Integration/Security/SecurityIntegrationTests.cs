using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.Tests.Integration.Support;

namespace neurozen.API.Tests.Integration.Security;

[Collection(IntegrationCollection.Name)]
public class SecurityIntegrationTests(IntegrationFixture fixture)
{
    private TestEnvironment Env => fixture.Environment;

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: todo endpoint que no es público exige un JWT; sin él la API no puede
    ///   saber quién pide los datos.
    /// - ¿Qué hace esta prueba?: llama al directorio sin token y comprueba 401 con un cuerpo JSON con message.
    /// </summary>
    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var client = Env.Api.Anonymous();

        // Act
        var response = await client.GetAsync("/api/v1/professionals");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await response.MessageAsync()));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un token vencido o modificado (por ejemplo, cambiando el rol a ADMIN en el
    ///   payload) no debe dar acceso; la firma y la expiración se validan siempre.
    /// - ¿Qué hace esta prueba?: firma un token válido pero ya vencido, y altera el payload de un token real sin
    ///   volver a firmarlo; ambos deben responder 401.
    /// </summary>
    [Theory]
    [InlineData("vencido")]
    [InlineData("alterado")]
    public async Task ProtectedEndpoint_WithExpiredOrTamperedToken_ShouldReturn401(string kind)
    {
        // Arrange
        var user = await Env.Api.AsUser();
        var token = kind == "vencido" ? ExpiredTokenFor(user.UserId) : TamperRole(user.Token, "ADMIN");
        var client = Env.Api.WithToken(token);

        // Act
        var response = await client.GetAsync($"/api/v1/users/{user.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: BOLA (OWASP API1); cambiar el id en la URL no debe exponer la cuenta de
    ///   otra persona.
    /// - ¿Qué hace esta prueba?: un trabajador pide la cuenta de otro y recibe 403 con mensaje, sin datos de la otra
    ///   cuenta en la respuesta.
    /// </summary>
    [Fact]
    public async Task GetUser_OfAnotherUser_ShouldReturn403()
    {
        // Arrange
        var ana = await Env.Api.AsUser();
        var luis = await Env.Api.AsUser("Luis Paredes");

        // Act
        var response = await ana.Client.GetAsync($"/api/v1/users/{luis.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(luis.Email, raw);
        Assert.False(string.IsNullOrWhiteSpace(await response.MessageAsync()));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: las citas son datos de salud; quien no participa en una cita no debe
    ///   saber siquiera que existe (404, no 403).
    /// - ¿Qué hace esta prueba?: un paciente reserva y otro paciente pide esa cita por id; recibe 404.
    /// </summary>
    [Fact]
    public async Task GetAppointment_OfOtherPatients_ShouldReturn404()
    {
        // Arrange
        var psychologist = await Env.Api.AsPsychologist();
        var ana = await Env.Api.AsUser();
        var luis = await Env.Api.AsUser("Luis Paredes");
        var booking = await TestApi.BookAsync(ana.Client, ana.UserId, psychologist.ProfessionalId,
            TestApi.SlotUtc(1, 10));
        await TestApi.EnsureStatusAsync(booking, HttpStatusCode.Created);
        var appointmentId = (await booking.ReadJsonAsync()).GetProperty("id").GetInt64();

        // Act
        var response = await luis.Client.GetAsync($"/api/v1/appointments/{appointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el panel de verificación es solo para ADMIN; un trabajador no puede ver
    ///   colegiaturas ni aprobar psicólogos.
    /// - ¿Qué hace esta prueba?: un USER llama al listado y a la aprobación del admin; ambos 403, y el psicólogo sigue
    ///   PENDING en la base.
    /// </summary>
    [Fact]
    public async Task Worker_CallingAdminEndpoints_ShouldReturn403()
    {
        // Arrange
        var worker = await Env.Api.AsUser();
        var pending = await Env.Api.AsPsychologist(verified: false);

        // Act
        var list = await worker.Client.GetAsync("/api/v1/admin/professionals?status=PENDING");
        var verify = await worker.Client.PatchAsync($"/api/v1/admin/professionals/{pending.ProfessionalId}/verify",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, verify.StatusCode);
        var status = await Env.QueryDatabaseAsync(db => db
            .Set<neurozen.API.Professionals.Domain.Model.Aggregates.Professional>()
            .Where(p => p.Id == pending.ProfessionalId).Select(p => p.VerificationStatus).SingleAsync());
        Assert.Equal(neurozen.API.Professionals.Domain.Model.ValueObjects.VerificationStatus.Pending, status);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un psicólogo no verificado no puede ver citas ni informes hasta que el
    ///   admin valide su colegiatura.
    /// - ¿Qué hace esta prueba?: un psicólogo PENDING pide sus propias citas y recibe 403 con el mensaje de cuenta no
    ///   verificada.
    /// </summary>
    [Fact]
    public async Task PendingPsychologist_CannotListAppointments()
    {
        // Arrange
        var pending = await Env.Api.AsPsychologist(verified: false);

        // Act
        var response = await pending.Client.GetAsync($"/api/v1/appointments?professionalId={pending.ProfessionalId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("no está verificada", await response.MessageAsync());
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: asignación masiva (OWASP API3); un usuario no puede convertirse en ADMIN
    ///   enviando "role" en el cuerpo al editar su perfil.
    /// - ¿Qué hace esta prueba?: actualiza el perfil enviando role ADMIN y un nombre nuevo; responde 200, el nombre
    ///   cambia y el rol sigue USER en la respuesta y en la base.
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithRoleInBody_ShouldNotChangeRole()
    {
        // Arrange
        var ana = await Env.Api.AsUser();

        // Act
        var response = await ana.Client.PutAsJsonAsync($"/api/v1/users/{ana.UserId}", new
        {
            fullName = "Ana Torres Ruiz", role = "ADMIN", verificationStatus = "VERIFIED", passwordHash = "x"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Equal("USER", body.GetProperty("role").GetString());
        Assert.Equal("Ana Torres Ruiz", body.GetProperty("fullName").GetString());
        var stored = await Env.QueryDatabaseAsync(db => db.Set<User>().SingleAsync(u => u.Id == ana.UserId));
        Assert.Equal(UserRole.User, stored.Role);
        Assert.NotEqual("x", stored.PasswordHash);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: exposición de datos (OWASP API3, PCI DSS); ninguna respuesta puede incluir
    ///   el hash de la contraseña, el CVV ni el número completo de la tarjeta.
    /// - ¿Qué hace esta prueba?: recorre las respuestas de registro, inicio de sesión, cuenta y suscripción (creación
    ///   y consulta) y comprueba que no tienen esas propiedades ni esos valores.
    /// </summary>
    [Fact]
    public async Task Responses_ShouldNeverContainPasswordHashCvvOrFullCard()
    {
        // Arrange
        const string fullCard = "4111111111111111";
        var email = TestApi.UniqueEmail("seguridad");
        var anonymous = Env.Api.Anonymous();

        // Act
        var signUp = await anonymous.PostAsJsonAsync("/api/v1/authentication/sign-up", new
        {
            email, password = TestApi.DefaultPassword, fullName = "Rosa Quispe", role = "USER", acceptedTerms = true
        });
        var token = (await signUp.ReadJsonAsync()).GetProperty("token").GetString()!;
        var userId = (await signUp.ReadJsonAsync()).GetProperty("id").GetInt64();
        var client = Env.Api.WithToken(token);
        var signIn = await Env.Api.Anonymous().PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { username = email, password = TestApi.DefaultPassword });
        var account = await client.GetAsync($"/api/v1/users/{userId}");
        var subscribe = await client.PostAsJsonAsync("/api/v1/subscriptions", new
        {
            userId, planId = 2, nameUser = "Rosa", lastNameUser = "Quispe", emailUser = email,
            numberCard = fullCard, expirationDate = "12/30", cvv = "987", isActive = false
        });
        var subscription = await client.GetAsync($"/api/v1/subscriptions/user/{userId}");

        // Assert
        foreach (var response in new[] { signUp, signIn, account, subscribe, subscription })
        {
            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri} → {response.StatusCode}");
            var raw = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(fullCard, raw);
            Assert.DoesNotContain("$2a$", raw);
            Assert.DoesNotContain("$2b$", raw);
            var names = PropertyNames(JsonDocument.Parse(raw).RootElement).ToList();
            Assert.DoesNotContain(names, n => n.Equals("passwordHash", StringComparison.OrdinalIgnoreCase) ||
                                              n.Equals("cvv", StringComparison.OrdinalIgnoreCase) ||
                                              n.Equals("numberCard", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un error de negocio debe llegar al frontend como 400/409 con un message
    ///   legible, nunca como un 500 genérico.
    /// - ¿Qué hace esta prueba?: registra un correo duplicado (409) y pide el rol ADMIN en el registro (400); ambos
    ///   responden JSON con message, y el 400 incluye el error del campo role.
    /// </summary>
    [Fact]
    public async Task BusinessError_ShouldReturnJsonMessageNot500()
    {
        // Arrange
        var existing = await Env.Api.AsUser();
        var anonymous = Env.Api.Anonymous();

        // Act
        var duplicate = await anonymous.PostAsJsonAsync("/api/v1/authentication/sign-up", new
        {
            email = existing.Email, password = TestApi.DefaultPassword, fullName = "Ana Torres", role = "USER",
            acceptedTerms = true
        });
        var admin = await anonymous.PostAsJsonAsync("/api/v1/authentication/sign-up", new
        {
            email = TestApi.UniqueEmail("intruso"), password = TestApi.DefaultPassword, fullName = "Carlos Rojas",
            role = "ADMIN", acceptedTerms = true
        });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("Ya existe una cuenta con este correo", await duplicate.MessageAsync());
        Assert.Equal("application/json", duplicate.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.BadRequest, admin.StatusCode);
        var adminBody = await admin.ReadJsonAsync();
        Assert.False(string.IsNullOrWhiteSpace(adminBody.GetProperty("message").GetString()));
        Assert.True(adminBody.GetProperty("errors").TryGetProperty("role", out _));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: fuerza bruta contra el inicio de sesión; tras 10 intentos por minuto
    ///   desde la misma IP la API debe frenar con 429.
    /// - ¿Qué hace esta prueba?: desde una misma IP hace 10 intentos con contraseña incorrecta (401) y comprueba que
    ///   el undécimo recibe 429 con message.
    /// </summary>
    [Fact]
    public async Task SignIn_RepeatedFailures_ShouldBeRateLimited()
    {
        // Arrange
        var user = await Env.Api.AsUser();
        var attacker = Env.Api.Anonymous(TestApi.NextIp());
        var body = new { username = user.Email, password = "Incorrecta1" };

        // Act
        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 10; attempt++)
            statuses.Add((await attacker.PostAsJsonAsync("/api/v1/authentication/sign-in", body)).StatusCode);
        var blocked = await attacker.PostAsJsonAsync("/api/v1/authentication/sign-in", body);

        // Assert
        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await blocked.MessageAsync()));
    }

    private static string ExpiredTokenFor(long userId)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("role", "USER")
            }),
            Issuer = TestEnvironment.JwtIssuer,
            Audience = TestEnvironment.JwtAudience,
            IssuedAt = now.AddHours(-3),
            NotBefore = now.AddHours(-3),
            Expires = now.AddHours(-1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestEnvironment.JwtSecret)),
                SecurityAlgorithms.HmacSha256)
        });
    }

    /// <summary>Changes the role claim in the payload and keeps the original signature (now invalid).</summary>
    private static string TamperRole(string token, string role)
    {
        var parts = token.Split('.');
        var payload = JsonNode.Parse(Base64UrlEncoder.Decode(parts[1]))!.AsObject();
        payload["role"] = role;
        parts[1] = Base64UrlEncoder.Encode(payload.ToJsonString());
        return string.Join('.', parts);
    }

    private static IEnumerable<string> PropertyNames(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    yield return property.Name;
                    foreach (var nested in PropertyNames(property.Value)) yield return nested;
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                foreach (var nested in PropertyNames(item))
                    yield return nested;
                break;
        }
    }
}
