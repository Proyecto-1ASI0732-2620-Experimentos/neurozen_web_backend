namespace neurozen.API.Shared.Domain.Model.Exceptions;

/// <summary>
///     Collects field errors so a request reports every problem at once,
///     then throws a single <see cref="ValidationFailedException" />.
/// </summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public ValidationErrors Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = [];
            _errors[field] = list;
        }

        list.Add(message);
        return this;
    }

    public ValidationErrors AddIf(bool condition, string field, string message)
    {
        if (condition) Add(field, message);
        return this;
    }

    /// <summary>Copies every collected error into <paramref name="target" />.</summary>
    public void CopyTo(ValidationErrors target)
    {
        foreach (var (field, messages) in _errors)
        foreach (var message in messages)
            target.Add(field, message);
    }

    public void ThrowIfAny(string message = "Hay datos inválidos en la solicitud")
    {
        if (!HasErrors) return;
        throw new ValidationFailedException(message,
            _errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
    }
}
