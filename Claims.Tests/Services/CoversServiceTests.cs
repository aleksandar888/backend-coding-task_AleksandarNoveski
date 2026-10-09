using Claims.Auditing;
using Claims.Logging;
using Claims.Models;
using Claims.Services;
using Moq;
using Xunit;

namespace Claims.Tests.Services;

public class CoversServiceTests
{
    private readonly Mock<ICoversRepository> _repository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<ICoverPremiumCalculator> _premiumCalculator = new();
    private readonly Mock<IErrorLoggingService> _errorLoggingService = new();
    private readonly CoversService _sut;

    public CoversServiceTests()
    {
        _sut = new CoversService(
            _repository.Object,
            _auditService.Object,
            _premiumCalculator.Object,
            _errorLoggingService.Object);
    }



    [Fact]
    public async Task DeleteAsync_WhenCoverDoesNotExist_ReturnsFalseAndAuditsDelete()
    {
        const string id = "cover-1";
        _repository
            .Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync((Cover?)null);

        var result = await _sut.DeleteAsync(id);

        Assert.False(result);
        _auditService.Verify(
            audit => audit.AuditCoverAsync(id, "DELETE"),
            Times.Once);
        _repository.Verify(
            repository => repository.DeleteAsync(It.IsAny<Cover>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCoverExists_DeletesCoverAndReturnsTrue()
    {
        const string id = "cover-1";
        var cover = new Cover();

        _repository
            .Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync(cover);

        var result = await _sut.DeleteAsync(id);

        Assert.True(result);
        _auditService.Verify(
            audit => audit.AuditCoverAsync(id, "DELETE"),
            Times.Once);
        _repository.Verify(
            repository => repository.DeleteAsync(cover),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CreatesCoverAndAuditsCreation()
    {
        var cover = new Cover();

        _repository
            .Setup(repository => repository.AddAsync(cover))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(cover);

        _auditService.Verify(
            audit => audit.AuditCoverAsync(cover.Id, "POST"),
            Times.Once);

        Assert.Same(cover, result);
        _repository.Verify(
            repository => repository.AddAsync(cover),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryReturnsCoverWithoutId_ReturnsCoverWithoutId()
    {
        var exception = new InvalidOperationException("Unable to create cover.");
        var cover = new Cover();

        _repository
             .Setup(repository => repository.AddAsync(cover))
             .ThrowsAsync(exception);

        var actualCover = await _sut.CreateAsync(cover);

        Assert.Null(actualCover.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCoversFromRepository()
    {
        IReadOnlyList<Cover> covers = [new Cover(), new Cover()];

        _repository
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(covers);

        var result = await _sut.GetAllAsync();

        Assert.Same(covers, result);
        _repository.Verify(
            repository => repository.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCoverExists_ReturnsCoverFromRepository()
    {
        const string id = "cover-1";
        var cover = new Cover();

        _repository
            .Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync(cover);

        var result = await _sut.GetByIdAsync(id);

        Assert.Same(cover, result);
        _repository.Verify(
            repository => repository.GetByIdAsync(id),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCoverDoesNotExist_ReturnsNull()
    {
        const string id = "missing-cover";

        _repository
            .Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync((Cover?)null);

        var result = await _sut.GetByIdAsync(id);

        Assert.Null(result);
        _repository.Verify(
            repository => repository.GetByIdAsync(id),
            Times.Once);
    }
}