using System.Threading.Channels;

namespace Claims.Auditing;

public enum AuditEntity
{
    Claim,
    Cover
}

public sealed record AuditEvent(
    AuditEntity Entity,
    string EntityId,
    string RequestType,
    DateTime Created);

public interface IAuditQueue
{
    ValueTask EnqueueAsync(AuditEvent auditEvent);

    IAsyncEnumerable<AuditEvent> ReadAllAsync(CancellationToken cancellationToken);
}

public sealed class AuditQueue : IAuditQueue
{
    private readonly Channel<AuditEvent> _channel =
        Channel.CreateBounded<AuditEvent>(new BoundedChannelOptions(1000)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    public ValueTask EnqueueAsync(AuditEvent auditEvent) =>
        _channel.Writer.WriteAsync(auditEvent);

    public IAsyncEnumerable<AuditEvent> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}