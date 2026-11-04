using System.Text.RegularExpressions;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.Professionals.Domain.Model.Aggregates;

/// <summary>
///     Professional profile of a user with role PSYCHOLOGIST (1:1 with User).
///     The display name comes from User.FullName; it is not duplicated here.
/// </summary>
public partial class Professional
{
    public const int LicenseNumberMaxLength = 6;
    public const int RejectionReasonMaxLength = 300;
    public const int BioMaxLength = 800;
    public const int AvailabilityMaxLength = 120;
    public const int ImageMaxLength = 500;

    protected Professional()
    {
    }

    /// <summary>Created at sign-up, always PENDING until an admin verifies it.</summary>
    public Professional(long userId, Specialty specialty, string licenseNumber)
    {
        if (!IsValidLicenseNumber(licenseNumber))
            throw new ArgumentException("License number must have 3 to 6 digits", nameof(licenseNumber));

        UserId = userId;
        Specialty = specialty;
        LicenseNumber = licenseNumber.Trim();
        VerificationStatus = VerificationStatus.Pending;
        Rating = 0;
        ReviewsCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public Specialty Specialty { get; private set; }
    /// <summary>Null only for deleted accounts (the license number is released).</summary>
    public string? LicenseNumber { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public long? VerifiedBy { get; private set; }
    public string? Bio { get; private set; }
    public int? ExperienceYears { get; private set; }
    public decimal? Price { get; private set; }
    public AvailabilitySchedule? AvailabilitySchedule { get; private set; }
    public string? Availability { get; private set; }

    /// <summary>Average of reviews, one decimal. Recomputed when a review is added.</summary>
    public decimal Rating { get; private set; }

    /// <summary>Number of reviews (exposed as "reviews" in JSON).</summary>
    public int ReviewsCount { get; private set; }

    public string? Image { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public bool IsVerified => VerificationStatus == VerificationStatus.Verified;

    public bool IsDeleted => DeletedAt is not null;

    /// <summary>Visible in the directory and bookable: verified and not deleted.</summary>
    public bool IsVisibleToPatients => IsVerified && !IsDeleted;

    /// <summary>
    ///     Replaces the profile with already validated values (PUT /professionals/me sends the whole profile).
    ///     Goes back to PENDING (and clears the rejection reason) when the license number or the full
    ///     name changes, or when the request was REJECTED (re-submission). Returns true in that case.
    /// </summary>
    public bool UpdateProfile(Specialty specialty, string licenseNumber, string? bio, int? experienceYears,
        decimal? price, AvailabilitySchedule availabilitySchedule, string availability, bool fullNameChanged)
    {
        var licenseChanged = !string.Equals(LicenseNumber, licenseNumber, StringComparison.Ordinal);
        var resubmitted = licenseChanged || fullNameChanged || VerificationStatus == VerificationStatus.Rejected;

        Specialty = specialty;
        LicenseNumber = licenseNumber;
        Bio = bio;
        ExperienceYears = experienceYears;
        Price = price;
        AvailabilitySchedule = availabilitySchedule;
        Availability = availability;

        if (resubmitted)
        {
            VerificationStatus = VerificationStatus.Pending;
            RejectionReason = null;
            VerifiedAt = null;
            VerifiedBy = null;
        }

        UpdatedAt = DateTime.UtcNow;
        return resubmitted;
    }

    /// <summary>Recomputed from the stored reviews after each new review (average rounded to 1 decimal).</summary>
    public void ApplyReviewStatistics(decimal averageRating, int reviewsCount)
    {
        Rating = Math.Round(averageRating, 1, MidpointRounding.AwayFromZero);
        ReviewsCount = reviewsCount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Admin approval after checking the license in the CPsP portal. Only from PENDING.</summary>
    public void Verify(long adminUserId, DateTime nowUtc)
    {
        if (VerificationStatus != VerificationStatus.Pending)
            throw new ConflictException(VerificationStatus == VerificationStatus.Verified
                ? "El profesional ya está verificado"
                : "Solo se pueden verificar solicitudes pendientes");

        VerificationStatus = VerificationStatus.Verified;
        VerifiedAt = nowUtc;
        VerifiedBy = adminUserId;
        RejectionReason = null;
        UpdatedAt = nowUtc;
    }

    /// <summary>Admin rejection with a reason (10-300 characters, validated by the caller). Only from PENDING.</summary>
    public void Reject(string reason, DateTime nowUtc)
    {
        if (VerificationStatus != VerificationStatus.Pending)
            throw new ConflictException(VerificationStatus == VerificationStatus.Rejected
                ? "La solicitud ya fue rechazada"
                : "Solo se pueden rechazar solicitudes pendientes");

        VerificationStatus = VerificationStatus.Rejected;
        RejectionReason = reason;
        VerifiedAt = null;
        VerifiedBy = null;
        UpdatedAt = nowUtc;
    }

    /// <summary>
    ///     Account deletion: removes the public profile data and releases the license number.
    ///     The row stays so past appointments and reviews keep a valid reference.
    /// </summary>
    public void MarkDeleted(DateTime nowUtc)
    {
        LicenseNumber = null;
        Bio = null;
        Image = null;
        Availability = null;
        AvailabilitySchedule = null;
        RejectionReason = null;
        DeletedAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    public static bool IsValidLicenseNumber(string? licenseNumber) =>
        licenseNumber is not null && LicenseNumberRegex().IsMatch(licenseNumber.Trim());

    [GeneratedRegex(@"^\d{3,6}$")]
    private static partial Regex LicenseNumberRegex();
}
