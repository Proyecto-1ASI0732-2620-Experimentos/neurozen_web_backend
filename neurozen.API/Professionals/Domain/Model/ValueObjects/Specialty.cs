namespace neurozen.API.Professionals.Domain.Model.ValueObjects;

public enum Specialty
{
    Clinical,
    Organizational,
    Educational,
    Family,
    ChildAdolescent,
    Health,
    Social,
    Neuropsychology
}

public static class SpecialtyExtensions
{
    /// <summary>Wire / storage value in camelCase: clinical, childAdolescent, ...</summary>
    public static string ToWire(this Specialty specialty)
    {
        var name = specialty.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    public static Specialty FromWire(string value) =>
        TryFromWire(value, out var specialty)
            ? specialty
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown specialty");

    public static bool TryFromWire(string? value, out Specialty specialty)
    {
        specialty = Specialty.Clinical;
        if (string.IsNullOrWhiteSpace(value)) return false;
        foreach (var candidate in Enum.GetValues<Specialty>())
            if (string.Equals(candidate.ToWire(), value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                specialty = candidate;
                return true;
            }

        return false;
    }
}
