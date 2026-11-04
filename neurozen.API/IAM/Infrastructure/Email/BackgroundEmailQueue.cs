using System.Threading.Channels;
using neurozen.API.IAM.Application.Internal.OutboundServices;

namespace neurozen.API.IAM.Infrastructure.Email;

/// <summary>In-memory queue consumed by <see cref="EmailDispatcherService" />.</summary>
public sealed class BackgroundEmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public ValueTask EnqueueAsync(EmailMessage message) => _channel.Writer.WriteAsync(message);

    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
