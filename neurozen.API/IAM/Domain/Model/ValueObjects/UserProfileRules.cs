namespace neurozen.API.IAM.Domain.Model.ValueObjects;

/// <summary>Allowed values and limits for the optional profile / onboarding fields.</summary>
public static class UserProfileRules
{
    public const int PhoneNumberMaxLength = 20;
    public const int AddressMaxLength = 200;
    public const int AvatarUrlMaxLength = 500;
    public const int OccupationMaxLength = 80;

    public static readonly IReadOnlyList<string> AgeRanges = ["18-24", "25-34", "35-44", "45-54", "55+"];
    public static readonly IReadOnlyList<string> WorkTypes = ["formal", "informal"];
}
