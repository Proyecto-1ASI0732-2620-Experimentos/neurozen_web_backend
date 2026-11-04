using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using neurozen.API.IAM.Application.Internal.OutboundServices;

namespace neurozen.API.IAM.Infrastructure.Email;

/// <summary>
///     Sends queued emails over SMTP (or to a pickup directory in development).
///     Logs never include recipients, links or tokens.
/// </summary>
public sealed class EmailDispatcherService(
    BackgroundEmailQueue queue,
    IOptions<EmailSettings> settings,
    ILogger<EmailDispatcherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendAsync(message, stoppingToken);
                logger.LogInformation("Transactional email dispatched");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError("Transactional email could not be sent ({ExceptionType})", exception.GetType().Name);
            }
        }
    }

    private async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var config = settings.Value;
        using var client = new SmtpClient();

        if (!string.IsNullOrWhiteSpace(config.PickupDirectory))
        {
            var directory = Path.GetFullPath(config.PickupDirectory);
            Directory.CreateDirectory(directory);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = directory;
        }
        else if (!string.IsNullOrWhiteSpace(config.Host))
        {
            client.Host = config.Host;
            client.Port = config.Port;
            client.EnableSsl = config.EnableSsl;
            if (!string.IsNullOrWhiteSpace(config.Username))
                client.Credentials = new NetworkCredential(config.Username, config.Password);
        }
        else
        {
            logger.LogWarning("Email is not configured (Email:Host or Email:PickupDirectory); message discarded");
            return;
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(config.From, config.FromName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false
        };
        mail.To.Add(message.To);

        await client.SendMailAsync(mail, cancellationToken);
    }
}
