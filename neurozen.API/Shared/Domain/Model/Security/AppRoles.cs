namespace neurozen.API.Shared.Domain.Model.Security;

/// <summary>
///     Role names as they travel in the JWT "role" claim and in JSON.
///     Used by [Authorize(Roles = ...)] across bounded contexts.
/// </summary>
public static class AppRoles
{
    public const string User = "USER";
    public const string Psychologist = "PSYCHOLOGIST";
    public const string Admin = "ADMIN";
}
