namespace Claims.Auditing;

public sealed class AuditBackgroundService : BackgroundService
{
    private readonly IAuditQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditBackgroundService> _logger;

    public AuditBackgroundService(
        IAuditQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var auditEvent in _queue.ReadAllAsync(stoppingToken))
        {
            var retryDelay = TimeSpan.FromMilliseconds(500);

            while (true)
            {
                try
                {
                    await PersistAsync(auditEvent, stoppingToken);
                    break;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to save audit event for {Entity} {EntityId}; retrying.",
                        auditEvent.Entity,
                        auditEvent.EntityId);

                    await Task.Delay(retryDelay, stoppingToken);
                    retryDelay = TimeSpan.FromSeconds(
                        Math.Min(retryDelay.TotalSeconds * 2, 30));
                }
            }
        }
    }

    private async Task PersistAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();

        switch (auditEvent.Entity)
        {
            case AuditEntity.Claim:
                context.ClaimAudits.Add(new ClaimAudit
                {
                    ClaimId = auditEvent.EntityId,
                    HttpRequestType = auditEvent.RequestType,
                    Created = auditEvent.Created
                });
                break;

            case AuditEntity.Cover:
                context.CoverAudits.Add(new CoverAudit
                {
                    CoverId = auditEvent.EntityId,
                    HttpRequestType = auditEvent.RequestType,
                    Created = auditEvent.Created
                });
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(auditEvent), auditEvent.Entity, "Unknown audit entity.");
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}