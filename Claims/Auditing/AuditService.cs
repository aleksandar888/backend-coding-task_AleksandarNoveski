namespace Claims.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly IAuditQueue _queue;

    public AuditService(IAuditQueue queue)
    {
        _queue = queue;
    }

    public Task AuditClaimAsync(string id, string requestType) =>
        _queue.EnqueueAsync(new AuditEvent(
            AuditEntity.Claim, id, requestType, DateTime.Now)).AsTask();

    public Task AuditCoverAsync(string id, string requestType) =>
        _queue.EnqueueAsync(new AuditEvent(
            AuditEntity.Cover, id, requestType, DateTime.Now)).AsTask();
}
