namespace neurozen.API.Professionals.Domain.Model.Entities;

/// <summary>
///     Audit trail of admin decisions on license verification: who (admin), when, to whom (professional), what.
///     Rows are never updated or deleted.
/// </summary>
public class VerificationAuditEntry
{
    public const string VerifiedAction = "VERIFIED";
    public const string RejectedAction = "REJECTED";

    protected VerificationAuditEntry()
    {
        Action = string.Empty;
    }

    public VerificationAuditEntry(long professionalId, long adminUserId, string action, string? reason,
        DateTime createdAt)
    {
        ProfessionalId = professionalId;
        AdminUserId = adminUserId;
        Action = action;
        Reason = reason;
        CreatedAt = createdAt;
    }

    public long Id { get; private set; }
    public long ProfessionalId { get; private set; }
    public long AdminUserId { get; private set; }
    public string Action { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
