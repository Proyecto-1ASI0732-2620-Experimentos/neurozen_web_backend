using neurozen.API.Shared.Domain.Model.Security;

namespace neurozen.API.IAM.Domain.Model.ValueObjects;

public enum UserRole
{
    User,
    Psychologist,
    Admin
}

public static class UserRoleExtensions
{
    /// <summary>Wire / storage value: USER, PSYCHOLOGIST, ADMIN.</summary>
    public static string ToWire(this UserRole role) => role switch
    {
        UserRole.User => AppRoles.User,
        UserRole.Psychologist => AppRoles.Psychologist,
        UserRole.Admin => AppRoles.Admin,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    public static UserRole FromWire(string value) => value.Trim().ToUpperInvariant() switch
    {
        AppRoles.User => UserRole.User,
        AppRoles.Psychologist => UserRole.Psychologist,
        AppRoles.Admin => UserRole.Admin,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown role")
    };

    public static bool TryFromWire(string? value, out UserRole role)
    {
        role = UserRole.User;
        if (string.IsNullOrWhiteSpace(value)) return false;
        switch (value.Trim().ToUpperInvariant())
        {
            case AppRoles.User: role = UserRole.User; return true;
            case AppRoles.Psychologist: role = UserRole.Psychologist; return true;
            case AppRoles.Admin: role = UserRole.Admin; return true;
            default: return false;
        }
    }
}
