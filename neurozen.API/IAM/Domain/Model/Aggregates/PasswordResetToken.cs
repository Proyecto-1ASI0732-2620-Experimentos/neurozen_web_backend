namespace neurozen.API.IAM.Domain.Model.Aggregates;

/// <summary>
///     One-time password reset token. Only the SHA-256 hash of the token is stored;
///     the raw value exists only in the email sent to the user.
/// </summary>
public class PasswordResetToken
{
    protected PasswordResetToken()
    {
        TokenHash = string.Empty;
    }

    public PasswordResetToken(long userId, string tokenHash, DateTime expiresAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsUsable(DateTime nowUtc) => UsedAt is null && ExpiresAt > nowUtc;

    /// <summary>Marks the token as consumed (used or invalidated by a newer one).</summary>
    public void MarkUsed(DateTime nowUtc) => UsedAt ??= nowUtc;
}
