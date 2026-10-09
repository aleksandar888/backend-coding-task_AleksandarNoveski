using Claims.Controllers;
using Claims.Models;
using Claims.Services.Covers;
using Claims.Services.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Claims.Tests.Controllers;

public class CoversControllerTests
{
    private const string TraceIdentifier = "trace-covers-123";
    private const string InvalidPremiumParametersMessage =
        "The provided parameters are invalid. Please provide valid start and end dates, ensure the start date is earlier than the end date, and specify a valid cover type.";

    [Fact]
    public void ComputePremium_WhenParametersAreValid_ReturnsPremium()
    {
        var startDate = new DateTime(2025, 1, 1);
        var endDate = startDate.AddDays(10);
        var coversService = new Mock<ICoversService>();
        coversService
            .Setup(service => service.ComputePremium(startDate, endDate, CoverType.Yacht))
            .Returns(123.45m);
        var controller = CreateController(coversService);

        var result = controller.ComputePremium(startDate, endDate, CoverType.Yacht);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(123.45m, ok.Value);
        coversService.Verify(service => service.ComputePremium(startDate, endDate, CoverType.Yacht), Times.Once);
    }

    public static IEnumerable<object[]> InvalidPremiumParameters => new[]
{
        new object[] { default(DateTime), new DateTime(2025, 1, 2), CoverType.Yacht },
        new object[] { new DateTime(2025, 1, 1), default(DateTime), CoverType.Yacht },
        new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 1, 1), CoverType.Yacht },
        new object[] { new DateTime(2025, 1, 2), new DateTime(2025, 1, 1), CoverType.Yacht },
        new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 1, 2), (CoverType)999 }
    };

    [Theory]
    [MemberData(nameof(InvalidPremiumParameters))]
    public void ComputePremium_WhenParametersAreInvalid_ReturnsBadRequest(
        DateTime startDate,
        DateTime endDate,
        CoverType coverType)
    {
        var coversService = new Mock<ICoversService>();
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(coversService, errorLogger);

        var result = controller.ComputePremium(startDate, endDate, coverType);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(InvalidPremiumParametersMessage, badRequest.Value);
        coversService.Verify(
            service => service.ComputePremium(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CoverType>()),
            Times.Never);
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
    public void ComputePremium_WhenServiceThrows_ReturnsInternalServerErrorAndLogsException()
    {
        var startDate = new DateTime(2025, 1, 1);
        var endDate = startDate.AddDays(1);
        var exception = new InvalidOperationException("premium calculation failed");
        var coversService = new Mock<ICoversService>();
        coversService
            .Setup(service => service.ComputePremium(startDate, endDate, CoverType.Yacht))
            .Throws(exception);
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(coversService, errorLogger);

        var result = controller.ComputePremium(startDate, endDate, CoverType.Yacht);

        AssertErrorResponse(
            result,
            "An unexpected error occurred while computing the cover premium. Please try again later.");
        VerifyLoggedError(
            errorLogger,
            exception,
            "An error occurred while computing the cover premium.",
            nameof(CoversController.ComputePremium));
    }

    [Fact]
    public async Task GetAsync_WhenCoversExist_ReturnsOkWithCovers()
    {
        var cover = CreateCover("cover-1");
        IReadOnlyList<Cover> covers = [cover];
        var coversService = new Mock<ICoversService>();
        coversService.Setup(service => service.GetAllAsync()).ReturnsAsync(covers);
        var controller = CreateController(coversService);

        var actionResult = await controller.GetAsync();

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Same(covers, ok.Value);
    }

    [Fact]
    public async Task GetAsync_WhenServiceThrows_ReturnsInternalServerErrorAndLogsException()
    {
        var exception = new InvalidOperationException("service failure");
        var coversService = new Mock<ICoversService>();
        coversService.Setup(service => service.GetAllAsync()).ThrowsAsync(exception);
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(coversService, errorLogger);

        var actionResult = await controller.GetAsync();

        AssertErrorResponse(
            actionResult.Result,
            "An unexpected error occurred while retrieving covers. Please try again later.");
        VerifyLoggedError(
            errorLogger,
            exception,
            "An error occurred while retrieving all covers.",
            nameof(CoversController.GetAsync));
    }

    [Fact]
    public async Task GetAsync_ById_WhenCoverExists_ReturnsOkWithCover()
    {
        var cover = CreateCover("cover-2");
        var coversService = new Mock<ICoversService>();
        coversService.Setup(service => service.GetByIdAsync(cover.Id)).ReturnsAsync(cover);
        var controller = CreateController(coversService);

        var actionResult = await controller.GetAsync(cover.Id);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Same(cover, ok.Value);
    }

    [Fact]
    public async Task GetAsync_ById_WhenCoverDoesNotExist_ReturnsNotFound()
    {
        const string coverId = "missing-cover";
        var coversService = new Mock<ICoversService>();
        coversService.Setup(service => service.GetByIdAsync(coverId)).ReturnsAsync((Cover?)null);
        var controller = CreateController(coversService);

        var actionResult = await controller.GetAsync(coverId);

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.Equal($"Cover with ID '{coverId}' was not found.", notFound.Value);
    }

    [Fact]
    public async Task GetAsync_ById_WhenServiceThrows_ReturnsInternalServerErrorAndLogsException()
    {
        const string coverId = "cover-3";
        var exception = new InvalidOperationException("service failure");
        var coversService = new Mock<ICoversService>();
        coversService.Setup(service => service.GetByIdAsync(coverId)).ThrowsAsync(exception);
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(coversService, errorLogger);

        var actionResult = await controller.GetAsync(coverId);

        AssertErrorResponse(
            actionResult.Result,
            "An unexpected error occurred while retrieving the cover. Please try again later.");
        VerifyLoggedError(
            errorLogger,
            exception,
            "An error occurred while retrieving a cover by ID.",
            nameof(CoversController.GetAsync));
    }

    [Fact]
    public async Task CreateAsync_WhenCoverIsCreated_ReturnsCreatedAtRouteAndMapsRequest()
    {
        var startDate = new DateTime(2025, 1, 1);
        var request = new CreateCoverRequest
        {
            StartDate = startDate,
            EndDate = startDate.AddMonths(6),
            Type = CoverType.Tanker
        };
        var createdCover = CreateCover("cover-created");
        Cover? submittedCover = null;
        var coversService = new Mock<ICoversService>();
        coversService
            .Setup(service => service.CreateAsync(It.IsAny<Cover>()))
            .Callback<Cover>(cover => submittedCover = cover)
            .ReturnsAsync(createdCover);
        var controller = CreateController(coversService);

        var actionResult = await controller.CreateAsync(request);

        var result = Assert.IsType<CreatedAtRouteResult>(actionResult);
        Assert.Equal("GetCoverById", result.RouteName);
        Assert.Equal(createdCover.Id, result.RouteValues!["id"]);
        Assert.Same(createdCover, result.Value);
        Assert.NotNull(submittedCover);
        Assert.Equal(request.StartDate, submittedCover.StartDate);
        Assert.Equal(request.EndDate, submittedCover.EndDate);
        Assert.Equal(request.Type, submittedCover.Type);
    }

    [Fact]
    public async Task CreateAsync_WhenCreatedCoverHasNoId_ReturnsBadRequest()
    {
        var coversService = new Mock<ICoversService>();
        coversService
            .Setup(service => service.CreateAsync(It.IsAny<Cover>()))
            .ReturnsAsync(CreateCover(null));
        var controller = CreateController(coversService);

        var actionResult = await controller.CreateAsync(CreateCoverRequest());

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(
            "The cover was not created. Please check the provided details and try again.",
            badRequest.Value);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceThrows_ReturnsInternalServerErrorAndLogsException()
    {
        var exception = new InvalidOperationException("service failure");
        var coversService = new Mock<ICoversService>();
        coversService.Setup(service => service.CreateAsync(It.IsAny<Cover>())).ThrowsAsync(exception);
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(coversService, errorLogger);

        var actionResult = await controller.CreateAsync(CreateCoverRequest());

        AssertErrorResponse(
            actionResult,
            "An unexpected error occurred while creating the cover. Please try again later.");
        VerifyLoggedError(
            errorLogger,
            exception,
            "An error occurred while creating a cover.",
            nameof(CoversController.CreateAsync));
    }

    [Fact]
    public async Task DeleteAsync_WhenCoverIsDeleted_ReturnsNoContent()
    {
        const string coverId = "cover-delete";
        var coversService = new Mock<ICoversService>();
        coversService.Setup(service => service.DeleteAsync(coverId)).ReturnsAsync(true);
        var controller = CreateController(coversService);

        var result = await controller.DeleteAsync(coverId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenCoverDoesNotExist_ReturnsNotFound()
    {
        const string coverId = "missing-cover";
        var coversService = new Mock<ICoversService>();
        coversService.Setup(service => service.DeleteAsync(coverId)).ReturnsAsync(false);
        var controller = CreateController(coversService);

        var result = await controller.DeleteAsync(coverId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Cover with ID '{coverId}' was not found.", notFound.Value);
    }

    [Fact]
    public async Task DeleteAsync_WhenServiceThrows_ReturnsInternalServerErrorAndLogsException()
    {
        const string coverId = "cover-delete";
        var exception = new InvalidOperationException("service failure");
        var coversService = new Mock<ICoversService>();
        coversService.Setup(service => service.DeleteAsync(coverId)).ThrowsAsync(exception);
        var errorLogger = CreateErrorLogger();
        var controller = CreateController(coversService, errorLogger);

        var result = await controller.DeleteAsync(coverId);

        AssertErrorResponse(result, "An error occurred while deleting the cover.");
        VerifyLoggedError(
            errorLogger,
            exception,
            "An error occurred while deleting a cover.",
            nameof(CoversController.DeleteAsync));
    }

    private static CoversController CreateController(
        Mock<ICoversService> coversService,
        Mock<IErrorLoggingService>? errorLogger = null)
    {
        var logger = errorLogger ?? CreateErrorLogger();
        return new CoversController(coversService.Object, logger.Object)
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
                nameof(CoversController),
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

    private static Cover CreateCover(string? id) => new()
    {
        Id = id!,
        StartDate = new DateTime(2025, 1, 1),
        EndDate = new DateTime(2025, 7, 1),
        Type = CoverType.Yacht,
        Premium = 250m
    };

    private static CreateCoverRequest CreateCoverRequest() => new()
    {
        StartDate = new DateTime(2025, 1, 1),
        EndDate = new DateTime(2025, 7, 1),
        Type = CoverType.Yacht
    };
}
