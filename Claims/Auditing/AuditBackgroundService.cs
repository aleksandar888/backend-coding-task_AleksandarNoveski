namespace Claims.Auditing;

public sealed class AuditBackgroundService : BackgroundService
{
    private readonly IAuditQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public AuditBackgroundService(
        IAuditQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditBackgroundService> logger,
        IConfiguration configuration)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var auditEvent in _queue.ReadAllAsync(stoppingToken))
        {
            if (auditEvent.Entity is not (AuditEntity.Claim or AuditEntity.Cover))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(auditEvent), auditEvent.Entity, "Unknown audit entity.");
            }

            var retryDelay = TimeSpan.FromMilliseconds(_configuration.GetValue<int>("Auditing:retryDelayMilliseconds"));
            var maxAttempts = _configuration.GetValue<int>("Auditing:MaxAttempts");

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
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
                    if (attempt == maxAttempts)
                    {
                        _logger.LogError(
                            exception,
                            "Failed to save audit event for {Entity} {EntityId} after {AttemptCount} attempts.",
                            auditEvent.Entity,
                            auditEvent.EntityId,
                            maxAttempts);
                        break;
                    }

                    _logger.LogWarning(
                        exception,
                        "Failed to save audit event for {Entity} {EntityId} on attempt {Attempt}; retrying.",
                        auditEvent.Entity,
                        auditEvent.EntityId,
                        attempt);

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