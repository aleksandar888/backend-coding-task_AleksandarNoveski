using Microsoft.Extensions.Logging;

namespace Claims.Logging;

public sealed class ErrorLoggingService : IErrorLoggingService
{
    private readonly ILogger<ErrorLoggingService> _logger;

    public ErrorLoggingService(ILogger<ErrorLoggingService> logger)
    {
        _logger = logger;
    }

    public Task LogErrorAsync(Exception exception, string message, string sourceType, string sourceMember, string? requestId)
    {
        _logger.LogError(
            exception,
            "Error in {SourceType}.{SourceMember} for request {RequestId}: {ErrorMessage}",
            sourceType,
            sourceMember,
            requestId ?? string.Empty,
            message);
        return Task.CompletedTask;
    }
}
