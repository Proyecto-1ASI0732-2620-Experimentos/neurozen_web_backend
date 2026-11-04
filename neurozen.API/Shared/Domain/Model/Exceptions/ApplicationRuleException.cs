namespace neurozen.API.Shared.Domain.Model.Exceptions;

/// <summary>
///     Base class for expected business errors. The global exception handler maps each
///     subclass to an HTTP status code; they are never reported as 500.
/// </summary>
public abstract class ApplicationRuleException(string message) : Exception(message);

/// <summary>400 — invalid input or a business rule that the request breaks.</summary>
public sealed class ValidationFailedException(
    string message,
    IReadOnlyDictionary<string, string[]>? errors = null) : ApplicationRuleException(message)
{
    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}

/// <summary>401 — only for wrong credentials on /authentication/*.</summary>
public sealed class InvalidCredentialsException(string message) : ApplicationRuleException(message);

/// <summary>403 — authenticated, but the resource or action belongs to someone else.</summary>
public sealed class ForbiddenOperationException(string message) : ApplicationRuleException(message);

/// <summary>404 — the resource does not exist (or must not be revealed).</summary>
public sealed class ResourceNotFoundException(string message) : ApplicationRuleException(message);

/// <summary>409 — duplicate or state conflict.</summary>
public sealed class ConflictException(string message) : ApplicationRuleException(message);
