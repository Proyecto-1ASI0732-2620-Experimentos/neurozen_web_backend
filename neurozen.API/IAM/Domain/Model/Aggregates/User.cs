using neurozen.API.IAM.Domain.Model.ValueObjects;

namespace neurozen.API.IAM.Domain.Model.Aggregates;

/// <summary>
///     The user aggregate. The email is the login. The password hash is never serialized:
///     REST resources are built from explicit DTOs.
///     A deleted account is kept as an anonymized tombstone (DeletedAt set) so that past
///     appointments and reviews of other users keep a valid reference.
/// </summary>
public class User
{
    public const int FullNameMaxLength = 120;
    public const string DeletedUserName = "Usuario eliminado";

    protected User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        FullName = string.Empty;
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public User(string email, string passwordHash, string fullName, UserRole role, DateTime acceptedTermsAt)
    {
        Email = EmailAddress.Normalize(email);
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        Role = role;
        AcceptedTermsAt = acceptedTermsAt;
        SecurityStamp = Guid.NewGuid().ToString("N");
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public long Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string SecurityStamp { get; private set; }
    public string FullName { get; private set; }
    public UserRole Role { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Address { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? AgeRange { get; private set; }
    public string? Occupation { get; private set; }
    public string? WorkType { get; private set; }
    public DateTime AcceptedTermsAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        RegenerateSecurityStamp();
        Touch();
    }

    public void ChangeFullName(string fullName)
    {
        FullName = fullName.Trim();
        Touch();
    }

    /// <summary>Replaces the editable profile with already validated values. Role and password are not editable here.</summary>
    public void UpdateProfile(string email, string fullName, string? phoneNumber, string? address, string? avatarUrl,
        DateOnly? dateOfBirth, string? ageRange, string? occupation, string? workType, bool emailChanged = false)
    {
        if (emailChanged)
            RegenerateSecurityStamp();

        Email = EmailAddress.Normalize(email);
        FullName = fullName.Trim();
        PhoneNumber = phoneNumber;
        Address = address;
        AvatarUrl = avatarUrl;
        DateOfBirth = dateOfBirth;
        AgeRange = ageRange;
        Occupation = occupation;
        WorkType = workType;
        Touch();
    }

    /// <summary>
    ///     Removes every personal field. The email is replaced by a unique placeholder (so the real
    ///     address can register again) and the password hash is cleared (no login is possible).
    /// </summary>
    public void Anonymize(DateTime nowUtc)
    {
        Email = EmailAddress.ForDeletedAccount(Id);
        PasswordHash = string.Empty;
        SecurityStamp = Guid.NewGuid().ToString("N");
        FullName = DeletedUserName;
        PhoneNumber = null;
        Address = null;
        AvatarUrl = null;
        DateOfBirth = null;
        AgeRange = null;
        Occupation = null;
        WorkType = null;
        DeletedAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    public void RegenerateSecurityStamp() => SecurityStamp = Guid.NewGuid().ToString("N");

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
