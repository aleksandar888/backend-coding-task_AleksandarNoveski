using Claims.Services.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Claims.Tests.Services;

public class ErrorLoggingServiceTests
{
    private readonly Mock<ILogger<ErrorLoggingService>> _logger = new();
    private readonly ErrorLoggingService _sut;

    public ErrorLoggingServiceTests()
    {
        _sut = new ErrorLoggingService(_logger.Object);
    }

    [Fact]
    public void LogErrorAsync_LogsErrorWithSourceRequestAndException()
    {
        var exception = new InvalidOperationException("Database unavailable.");

        var result = _sut.LogErrorAsync(
            exception,
            "Unable to load claims.",
            "ClaimsController",
            "GetAll",
            "request-123");

        Assert.True(result.IsCompletedSuccessfully);
        AssertLoggedError(
            exception,
            "Error in ClaimsController.GetAll for request request-123: Unable to load claims.");
    }

    [Fact]
    public async Task LogErrorAsync_WhenRequestIdIsNull_LogsEmptyRequestId()
    {
        var exception = new Exception("Unexpected failure.");

        await _sut.LogErrorAsync(
            exception,
            "Operation failed.",
            "ClaimsService",
            "Create",
            null);

        AssertLoggedError(
            exception,
            "Error in ClaimsService.Create for request : Operation failed.");
    }

    private void AssertLoggedError(Exception exception, string expectedMessage)
    {
        var invocation = Assert.Single(_logger.Invocations);

        Assert.Equal(LogLevel.Error, invocation.Arguments[0]);
        Assert.Equal(expectedMessage, invocation.Arguments[2]?.ToString());
        Assert.Same(exception, invocation.Arguments[3]);
    }
}
