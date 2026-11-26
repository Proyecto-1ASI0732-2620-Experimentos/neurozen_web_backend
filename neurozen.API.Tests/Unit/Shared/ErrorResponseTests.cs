using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;

namespace neurozen.API.Tests.Unit.Shared;

/// <summary>
///     El cuerpo de error nunca lleva message null, "null", "undefined", un objeto o un texto técnico de .NET (en la
///     interfaz llegó a verse «null» en el inicio de sesión).
/// </summary>
public class ErrorResponseTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: ningún valor técnico puede llegar como mensaje.
    /// - ¿Qué hace esta prueba?: construye respuestas con valores problemáticos y comprueba el texto por defecto.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("NULL")]
    [InlineData("undefined")]
    [InlineData("{\"error\":1}")]
    [InlineData("[1]")]
    [InlineData("Exception of type 'System.Exception' was thrown.")]
    public void Message_NeverTechnical(string? message)
    {
        // Act
        var response = new ErrorResponse(message);

        // Assert
        Assert.Equal(ErrorResponse.DefaultMessage, response.Message);
        Assert.Null(response.Errors);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un mensaje legible se conserva y los errores por campo también se limpian.
    /// - ¿Qué hace esta prueba?: serializa y comprueba message, clave vacía → "body" y valores nulos → "Valor no válido".
    /// </summary>
    [Fact]
    public void ReadableMessage_IsKept_AndFieldErrorsAreCleaned()
    {
        // Arrange
        var errors = new Dictionary<string, string[]> { [""] = ["null"], ["email"] = ["Correo inválido"] };

        // Act
        var json = JsonSerializer.Serialize(new ErrorResponse("  Correo o contraseña incorrectos ", errors), Web);
        using var document = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("Correo o contraseña incorrectos", document.RootElement.GetProperty("message").GetString());
        Assert.Equal("Valor no válido", document.RootElement.GetProperty("errors").GetProperty("body")[0].GetString());
        Assert.Equal("Correo inválido", document.RootElement.GetProperty("errors").GetProperty("email")[0].GetString());
        Assert.False(JsonSerializer.SerializeToElement(new ErrorResponse("x"), Web).TryGetProperty("errors", out _));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: cada código tiene un texto por defecto en español.
    /// - ¿Qué hace esta prueba?: recorre los códigos usados por la API.
    /// </summary>
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(405)]
    [InlineData(409)]
    [InlineData(413)]
    [InlineData(415)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(418)]
    public void ForStatus_IsAlwaysReadable(int status) =>
        Assert.True(ErrorResponse.IsReadable(ErrorResponse.ForStatus(status)));

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: una excepción de negocio sin mensaje (o con el texto por defecto de .NET)
    ///   no puede llegar al cliente como tal.
    /// - ¿Qué hace esta prueba?: pasa por el manejador global excepciones con mensaje nulo y comprueba el texto por
    ///   código y el estado.
    /// </summary>
    [Theory]
    [InlineData(typeof(InvalidCredentialsException), 401)]
    [InlineData(typeof(ValidationFailedException), 400)]
    [InlineData(typeof(ForbiddenOperationException), 403)]
    [InlineData(typeof(ResourceNotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    public async Task GlobalHandler_NullMessage_UsesReadableTextForStatus(Type exceptionType, int status)
    {
        // Arrange
        var exception = exceptionType == typeof(ValidationFailedException)
            ? new ValidationFailedException(null!)
            : (Exception)Activator.CreateInstance(exceptionType, [null])!;
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        // Act
        var handled = await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(status, context.Response.StatusCode);
        Assert.Equal(ErrorResponse.ForStatus(status), await ReadMessageAsync(context));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un 413 de Kestrel debe explicar el problema, no solo «no válida».
    /// - ¿Qué hace esta prueba?: maneja BadHttpRequestException 413 y 400.
    /// </summary>
    [Fact]
    public async Task GlobalHandler_BadHttpRequest_UsesStatusText()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var tooLarge = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var bad = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        // Act
        await handler.TryHandleAsync(tooLarge, new BadHttpRequestException("x", 413), CancellationToken.None);
        await handler.TryHandleAsync(bad, new BadHttpRequestException("x"), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorResponse.ForStatus(413), await ReadMessageAsync(tooLarge));
        Assert.Equal("La solicitud no es válida", await ReadMessageAsync(bad));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: [ApiController] respondía ProblemDetails sin "message" en 415/NotFound().
    /// - ¿Qué hace esta prueba?: usa la fábrica de errores de cliente y comprueba el cuerpo { message }.
    /// </summary>
    [Fact]
    public void ClientErrorFactory_ReturnsMessageBody()
    {
        // Act
        var result = new ErrorResponseClientErrorFactory().GetClientError(new ActionContext(),
            new UnsupportedMediaTypeResult());

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(415, objectResult.StatusCode);
        Assert.Equal(ErrorResponse.ForStatus(415), Assert.IsType<ErrorResponse>(objectResult.Value).Message);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: una respuesta de error vacía (ruta inexistente) debe llevar mensaje, pero
    ///   no se tocan respuestas correctas, ya escritas o de HEAD.
    /// - ¿Qué hace esta prueba?: aplica la red de seguridad a cuatro respuestas distintas.
    /// </summary>
    [Fact]
    public async Task EmptyErrorBody_OnlyFillsEmptyErrorResponses()
    {
        // Arrange
        HttpContext Context(int status, string method = "GET", string? contentType = null)
        {
            var context = new DefaultHttpContext { Response = { Body = new MemoryStream(), StatusCode = status } };
            context.Request.Method = method;
            context.Response.ContentType = contentType;
            return context;
        }

        var notFound = Context(404);
        var ok = Context(204);
        var head = Context(404, "HEAD");
        var withBody = Context(403, contentType: "application/json");

        // Act
        foreach (var context in new[] { notFound, ok, head, withBody })
            await EmptyErrorBodyMiddleware.WriteAsync(context);

        // Assert
        Assert.Equal(ErrorResponse.ForStatus(404), await ReadMessageAsync(notFound));
        Assert.Equal(0, ok.Response.Body.Length);
        Assert.Equal(0, head.Response.Body.Length);
        Assert.Equal(0, withBody.Response.Body.Length);
    }

    private static async Task<string?> ReadMessageAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.GetProperty("message").GetString();
    }
}
