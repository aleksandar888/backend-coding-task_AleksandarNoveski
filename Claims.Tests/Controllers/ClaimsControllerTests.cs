using Claims.Controllers;
using Claims.Models;
using Claims.Services.Claims;
using Claims.Services.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace Claims.Tests.Controllers;

public class ClaimsControllerTests
{
    private const string TraceIdentifier = "trace-123";

    [Fact]
    public async Task Get_Claims()
    {
        var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(_ =>
            { });

        var client = application.CreateClient();

        var response = await client.GetAsync("/Claims");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAsync_WhenClaimsExist_ReturnsOkWithClaims()
    {
        var claim = CreateClaim("claim-1");
        IReadOnlyList<Claim> claims = [claim];
        var claimsService = new Mock<IClaimsService>();
        claimsService.Setup(service => service.GetAllAsync()).ReturnsAsync(claims);
        var controller = CreateController(claimsService);

        var actionResult = await controller.GetAsync();

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Same(claims, result.Value);
    }

    [Fact]
    public async Task GetAsync_WhenServiceThrows_ReturnsInternalServerErrorAndLogsException()
    {
        var exception = new InvalidOperationException("service failure");
        var claimsService = new Mock<IClaimsService>();
        claimsService.Setup(service => service.GetAllAsync()).ThrowsAsync(exception);
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(claimsService, errorLogger);

        var actionResult = await controller.GetAsync();

        AssertErrorResponse(
            actionResult.Result,
            "An unexpected error occurred while retrieving claims. Please try again later.");
        VerifyLoggedError(
            errorLogger,
            exception,
            "An error occurred while retrieving all claims.",
            nameof(ClaimsController.GetAsync));
    }

    [Fact]
    public async Task GetAsync_ById_WhenClaimExists_ReturnsOkWithClaim()
    {
        var claim = CreateClaim("claim-2");
        var claimsService = new Mock<IClaimsService>();
        claimsService.Setup(service => service.GetByIdAsync(claim.Id)).ReturnsAsync(claim);
        var controller = CreateController(claimsService);

        var actionResult = await controller.GetAsync(claim.Id);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Same(claim, result.Value);
    }

    [Fact]
    public async Task GetAsync_ById_WhenClaimDoesNotExist_ReturnsNotFound()
    {
        const string claimId = "missing-claim";
        var claimsService = new Mock<IClaimsService>();
        claimsService.Setup(service => service.GetByIdAsync(claimId)).ReturnsAsync((Claim?)null);
        var controller = CreateController(claimsService);

        var actionResult = await controller.GetAsync(claimId);

        var result = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.Equal($"Claim with ID '{claimId}' was not found.", result.Value);
    }

    [Fact]
    public async Task GetAsync_ById_WhenServiceThrows_ReturnsInternalServerErrorAndLogsException()
    {
        const string claimId = "claim-3";
        var exception = new InvalidOperationException("service failure");
        var claimsService = new Mock<IClaimsService>();
        claimsService.Setup(service => service.GetByIdAsync(claimId)).ThrowsAsync(exception);
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(claimsService, errorLogger);

        var actionResult = await controller.GetAsync(claimId);

        AssertErrorResponse(
            actionResult.Result,
            "An unexpected error occurred while retrieving the claim. Please try again later.");
        VerifyLoggedError(
            errorLogger,
            exception,
            "An error occurred while retrieving a claim by ID.",
            nameof(ClaimsController.GetAsync));
    }

    [Fact]
    public async Task CreateAsync_WhenServiceCreatesClaim_ReturnsCreatedAtRouteAndMapsRequest()
    {
        var request = new CreateClaimRequest
        {
            CoverId = "cover-1",
            Name = "Damaged hull",
            Type = ClaimType.Collision,
            DamageCost = 1250.50m
        };
        var createdClaim = CreateClaim("claim-created");
        Claim? submittedClaim = null;
        var claimsService = new Mock<IClaimsService>();
        claimsService
            .Setup(service => service.CreateAsync(It.IsAny<Claim>()))
            .Callback<Claim>(claim => submittedClaim = claim)
            .ReturnsAsync((createdClaim, (string?)null));
        var beforeCall = DateTime.Now;
        var controller = CreateController(claimsService);

        var actionResult = await controller.CreateAsync(request);

        var afterCall = DateTime.Now;
        var result = Assert.IsType<CreatedAtRouteResult>(actionResult);
        Assert.Equal("GetClaimById", result.RouteName);
        Assert.Equal(createdClaim.Id, result.RouteValues!["id"]);
        Assert.Same(createdClaim, result.Value);
        Assert.NotNull(submittedClaim);
        Assert.Equal(request.CoverId, submittedClaim.CoverId);
        Assert.Equal(request.Name, submittedClaim.Name);
        Assert.Equal(request.Type, submittedClaim.Type);
        Assert.Equal(request.DamageCost, submittedClaim.DamageCost);
        Assert.InRange(submittedClaim.Created, beforeCall, afterCall);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceReturnsError_ReturnsBadRequest()
    {
        const string error = "Cover was not found.";
        var claimsService = new Mock<IClaimsService>();
        claimsService
            .Setup(service => service.CreateAsync(It.IsAny<Claim>()))
            .ReturnsAsync(((Claim?)null, error));
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(claimsService, errorLogger);

        var actionResult = await controller.CreateAsync(CreateClaimRequest());

        var result = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(error, result.Value);
        errorLogger.Verify(
            logger => logger.LogErrorAsync(
                It.IsAny<Exception>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceThrows_ReturnsInternalServerErrorAndLogsException()
    {
        var exception = new InvalidOperationException("service failure");
        var claimsService = new Mock<IClaimsService>();
        claimsService.Setup(service => service.CreateAsync(It.IsAny<Claim>())).ThrowsAsync(exception);
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(claimsService, errorLogger);

        var actionResult = await controller.CreateAsync(CreateClaimRequest());

        AssertErrorResponse(
            actionResult,
            "An unexpected error occurred while creating the claim. Please try again later.");
        VerifyLoggedError(
            errorLogger,
            exception,
            "An error occurred while creating a claim.",
            nameof(ClaimsController.CreateAsync));
    }

    [Fact]
    public async Task DeleteAsync_WhenClaimIsDeleted_ReturnsNoContent()
    {
        const string claimId = "claim-delete";
        var claimsService = new Mock<IClaimsService>();
        claimsService.Setup(service => service.DeleteAsync(claimId)).ReturnsAsync(true);
        var controller = CreateController(claimsService);

        var result = await controller.DeleteAsync(claimId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenClaimDoesNotExist_ReturnsNotFound()
    {
        const string claimId = "missing-claim";
        var claimsService = new Mock<IClaimsService>();
        claimsService.Setup(service => service.DeleteAsync(claimId)).ReturnsAsync(false);
        var controller = CreateController(claimsService);

        var result = await controller.DeleteAsync(claimId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Claim with ID '{claimId}' was not found.", notFound.Value);
    }

    [Fact]
    public async Task DeleteAsync_WhenServiceThrows_ReturnsInternalServerErrorAndLogsException()
    {
        const string claimId = "claim-delete";
        var exception = new InvalidOperationException("service failure");
        var claimsService = new Mock<IClaimsService>();
        claimsService.Setup(service => service.DeleteAsync(claimId)).ThrowsAsync(exception);
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(claimsService, errorLogger);

        var result = await controller.DeleteAsync(claimId);

        AssertErrorResponse(
            result,
            "An unexpected error occurred while deleting the claim. Please try again later.");
        VerifyLoggedError(
            errorLogger,
            exception,
            "An error occurred while deleting a claim.",
            nameof(ClaimsController.DeleteAsync));
    }

    private static ClaimsController CreateController(
        Mock<IClaimsService> claimsService,
        Mock<IErrorLoggingService>? errorLogger = null)
    {
        var logger = errorLogger ?? CreateErrorLogger();
        return new ClaimsController(claimsService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = TraceIdentifier }
            }
        };
    }

    private static Mock<IErrorLoggingService> CreateErrorLogger()
    {
        var errorLogger = new Mock<IErrorLoggingService>();
        errorLogger
            .Setup(logger => logger.LogErrorAsync(
                It.IsAny<Exception>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        return errorLogger;
    }

    private static void VerifyLoggedError(
        Mock<IErrorLoggingService> errorLogger,
        Exception exception,
        string message,
        string sourceMember)
    {
        errorLogger.Verify(
            logger => logger.LogErrorAsync(
                exception,
                message,
                nameof(ClaimsController),
                sourceMember,
                TraceIdentifier),
            Times.Once);
    }

    private static void AssertErrorResponse(IActionResult? actionResult, string expectedMessage)
    {
        var result = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal(expectedMessage, result.Value);
    }

    private static Claim CreateClaim(string id) => new()
    {
        Id = id,
        CoverId = "cover-1",
        Name = "Test claim",
        Type = ClaimType.Collision,
        DamageCost = 500m,
        Created = new DateTime(2025, 1, 1)
    };

    private static CreateClaimRequest CreateClaimRequest() => new()
    {
        CoverId = "cover-1",
        Name = "Test claim",
        Type = ClaimType.Collision,
        DamageCost = 500m
    };
}
