namespace neurozen.API.IAM.Interfaces.REST.Resources;

/// <summary>
///     GET /users/{id} (owner only). Never includes the password hash. "dni" is the owner's own DNI
///     ("" for accounts created before the DNI was required); it is not editable through PUT /users/{id}.
/// </summary>
public record UserResource(
    long Id,
    string Email,
    string FullName,
    string Role,
    string PhoneNumber,
    string Address,
    string AvatarUrl,
    DateOnly? DateOfBirth,
    string AgeRange,
    string Occupation,
    string WorkType,
    DateTime CreatedAt,
    string Dni = "");
