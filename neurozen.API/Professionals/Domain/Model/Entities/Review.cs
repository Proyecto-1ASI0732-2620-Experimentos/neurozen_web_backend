namespace neurozen.API.Professionals.Domain.Model.Entities;

/// <summary>
///     Rating of a COMPLETED appointment by its patient (one review per appointment).
///     Public view shows only the abbreviated patient name ("Ana T."), never the email.
/// </summary>
public class Review
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int CommentMaxLength = 400;

    protected Review()
    {
    }

    public Review(long professionalId, long appointmentId, long patientId, int rating, string? comment)
    {
        ProfessionalId = professionalId;
        AppointmentId = appointmentId;
        PatientId = patientId;
        Rating = rating;
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }

    public long Id { get; private set; }
    public long ProfessionalId { get; private set; }
    public long AppointmentId { get; private set; }
    public long PatientId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
