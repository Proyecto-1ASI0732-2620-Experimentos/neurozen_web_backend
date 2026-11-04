namespace neurozen.API.IAM.Application.Internal.OutboundServices;

public record EmailMessage(string To, string Subject, string TextBody);

/// <summary>
///     Emails are sent in the background so the HTTP response time does not depend on
///     whether a message was sent (prevents account enumeration by timing).
/// </summary>
public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message);
}
