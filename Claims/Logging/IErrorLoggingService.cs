namespace Claims.Logging;

public interface IErrorLoggingService
{
    Task LogErrorAsync(Exception exception, string message, string sourceType, string sourceMember, string? requestId = null);
}
