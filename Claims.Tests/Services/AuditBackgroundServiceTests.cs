using System.Runtime.CompilerServices;
using Claims.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Claims.Tests.Services;

public class AuditBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenClaimAuditIsQueued_PersistsClaimAudit()
    {
        var auditEvent = new AuditEvent(
            AuditEntity.Claim,
            "claim-123",
            "POST",
            new DateTime(2025, 4, 5, 10, 30, 0, DateTimeKind.Utc));
        var (provider, contexts) = CreateScopeProvider();
        using (provider)
        using (var service = CreateService([auditEvent], provider))
        {
            await service.StartAsync(CancellationToken.None);
            await service.ExecuteTask!;
        }

        var context = Assert.Single(contexts);
        var audit = Assert.Single(context.AddedClaimAudits);
        Assert.Equal(auditEvent.EntityId, audit.ClaimId);
        Assert.Equal(auditEvent.RequestType, audit.HttpRequestType);
        Assert.Equal(auditEvent.Created, audit.Created);
        Assert.Equal(1, context.SaveChangesCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCoverAuditIsQueued_PersistsCoverAudit()
    {
        var auditEvent = new AuditEvent(
            AuditEntity.Cover,
            "cover-123",
            "DELETE",
            new DateTime(2025, 5, 6, 11, 45, 0, DateTimeKind.Utc));
        var (provider, contexts) = CreateScopeProvider();
        using (provider)
        using (var service = CreateService([auditEvent], provider))
        {
            await service.StartAsync(CancellationToken.None);
            await service.ExecuteTask!;
        }

        var context = Assert.Single(contexts);
        var audit = Assert.Single(context.AddedCoverAudits);
        Assert.Equal(auditEvent.EntityId, audit.CoverId);
        Assert.Equal(auditEvent.RequestType, audit.HttpRequestType);
        Assert.Equal(auditEvent.Created, audit.Created);
        Assert.Equal(1, context.SaveChangesCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersistenceFails_RetriesUntilSaveSucceeds()
    {
        var auditEvent = new AuditEvent(AuditEntity.Claim, "claim-456", "PUT", DateTime.UtcNow);
        var (provider, contexts) = CreateScopeProvider(index =>
        {
            var context = new RecordingAuditContext();
            if (index == 0)
            {
                context.SaveChangesBehavior = _ =>
                    Task.FromException(new InvalidOperationException("Temporary database failure."));
            }

            return context;
        });
        var logger = new Mock<ILogger<AuditBackgroundService>>();
        using (provider)
        using (var service = new AuditBackgroundService(
                   new TestAuditQueue([auditEvent]),
                   provider.GetRequiredService<IServiceScopeFactory>(),
                   logger.Object,
                   CreateConfiguration()))
        {
            await service.StartAsync(CancellationToken.None);
            await service.ExecuteTask!;
        }

        Assert.Equal(2, contexts.Count);
        Assert.Equal(1, contexts[0].SaveChangesCount);
        Assert.Equal(1, contexts[1].SaveChangesCount);
        Assert.Single(contexts[1].AddedClaimAudits);

        var log = Assert.Single(logger.Invocations);
        Assert.Equal(LogLevel.Warning, log.Arguments[0]);
        Assert.Contains("Failed to save audit event for Claim claim-456 on attempt 1; retrying.", log.Arguments[2]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationOccursDuringPersistence_StopsWithoutRetrying()
    {
        var saveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = new RecordingAuditContext
        {
            SaveChangesBehavior = async cancellationToken =>
            {
                saveStarted.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
        };
        var (provider, _) = CreateScopeProvider(_ => context);
        var logger = new Mock<ILogger<AuditBackgroundService>>();
        using (provider)
        using (var service = new AuditBackgroundService(
                   new TestAuditQueue([new AuditEvent(AuditEntity.Claim, "claim-789", "POST", DateTime.UtcNow)]),
                   provider.GetRequiredService<IServiceScopeFactory>(),
                   logger.Object,
                   CreateConfiguration()))
        {
            await service.StartAsync(CancellationToken.None);
            await saveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await service.StopAsync(stopTimeout.Token);
        }

        Assert.Equal(1, context.SaveChangesCount);
        Assert.Empty(logger.Invocations);
    }

    private static AuditBackgroundService CreateService(
        IEnumerable<AuditEvent> auditEvents,
        ServiceProvider provider) =>
        new(
            new TestAuditQueue(auditEvents),
            provider.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<ILogger<AuditBackgroundService>>(),
            CreateConfiguration());

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auditing:retryDelayMilliseconds"] = "0",
                ["Auditing:MaxAttempts"] = "3"
            })
            .Build();

    private static (ServiceProvider Provider, List<RecordingAuditContext> Contexts) CreateScopeProvider(
        Func<int, RecordingAuditContext>? contextFactory = null)
    {
        var contexts = new List<RecordingAuditContext>();
        var services = new ServiceCollection();
        services.AddScoped<AuditContext>(_ =>
        {
            var context = contextFactory?.Invoke(contexts.Count) ?? new RecordingAuditContext();
            contexts.Add(context);
            return context;
        });

        return (services.BuildServiceProvider(), contexts);
    }

    private sealed class TestAuditQueue(IEnumerable<AuditEvent> auditEvents) : IAuditQueue
    {
        public ValueTask EnqueueAsync(AuditEvent auditEvent) => ValueTask.CompletedTask;

        public async IAsyncEnumerable<AuditEvent> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var auditEvent in auditEvents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return auditEvent;
                await Task.Yield();
            }
        }
    }

    private sealed class RecordingAuditContext : AuditContext
    {
        private readonly Mock<DbSet<ClaimAudit>> _claimAudits = new();
        private readonly Mock<DbSet<CoverAudit>> _coverAudits = new();

        public RecordingAuditContext()
            : base(new DbContextOptionsBuilder<AuditContext>().Options)
        {
            ClaimAudits = _claimAudits.Object;
            CoverAudits = _coverAudits.Object;

            _claimAudits
                .Setup(set => set.Add(It.IsAny<ClaimAudit>()))
                .Callback<ClaimAudit>(AddedClaimAudits.Add)
                .Returns((ClaimAudit _) => null!);
            _coverAudits
                .Setup(set => set.Add(It.IsAny<CoverAudit>()))
                .Callback<CoverAudit>(AddedCoverAudits.Add)
                .Returns((CoverAudit _) => null!);
        }

        public List<ClaimAudit> AddedClaimAudits { get; } = [];
        public List<CoverAudit> AddedCoverAudits { get; } = [];
        public int SaveChangesCount { get; private set; }
        public Func<CancellationToken, Task>? SaveChangesBehavior { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            if (SaveChangesBehavior is not null)
            {
                await SaveChangesBehavior(cancellationToken);
            }

            return 1;
        }
    }
}
