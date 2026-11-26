namespace neurozen.API.Safety.Application.Internal.Configuration;

/// <summary>
///     "EmergencyResources" section. VerifiedAt ("YYYY-MM-DD"): date on which the team checked the phone lines against
///     the official source. Not a secret; empty by default, in which case verifiedAt is sent as null.
/// </summary>
public class EmergencyResourcesSettings
{
    public string? VerifiedAt { get; set; }
}
