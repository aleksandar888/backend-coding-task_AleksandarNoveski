using Claims.Auditing;
using Claims.Data;
using Claims.Models;
using Claims.Services.Claims;
using Moq;
using Xunit;

namespace Claims.Tests.Services;

public class ClaimsServiceTests
{
    private readonly Mock<IClaimsRepository> _repository = new();
    private readonly Mock<ICoversRepository> _coversRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly ClaimsService _sut;

    public ClaimsServiceTests()
    {
        _sut = new ClaimsService(
            _repository.Object,
            _coversRepository.Object,
            _auditService.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsClaimsFromRepository()
    {
        IReadOnlyList<Claim> claims = [new Claim(), new Claim()];
        _repository
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(claims);

        var result = await _sut.GetAllAsync();

        Assert.Same(claims, result);
        _repository.Verify(repository => repository.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsClaimFromRepository()
    {
        const string id = "claim-1";
        var claim = new Claim { Id = id };
        _repository
            .Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync(claim);

        var result = await _sut.GetByIdAsync(id);

        Assert.Same(claim, result);
        _repository.Verify(repository => repository.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenClaimDoesNotExist_ReturnsNull()
    {
        const string id = "missing-claim";
        _repository
            .Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync((Claim?)null);

        var result = await _sut.GetByIdAsync(id);

        Assert.Null(result);
        _repository.Verify(repository => repository.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCoverDoesNotExist_ReturnsErrorWithoutCreatingOrAuditing()
    {
        const string coverId = "missing-cover";
        var claim = new Claim { CoverId = coverId };
        _coversRepository
            .Setup(repository => repository.GetByIdAsync(coverId))
            .ReturnsAsync((Cover?)null);

        var (createdClaim, error) = await _sut.CreateAsync(claim);

        Assert.Null(createdClaim);
        Assert.Equal("Please select a valid cover.", error);
        _repository.Verify(repository => repository.AddAsync(It.IsAny<Claim>()), Times.Never);
        _auditService.Verify(audit => audit.AuditClaimAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task CreateAsync_WhenClaimDateIsOutsideCoverPeriod_ReturnsErrorWithoutCreating(int dayOffset)
    {
        const string coverId = "cover-1";
        var cover = new Cover
        {
            StartDate = new DateTime(2025, 1, 10),
            EndDate = new DateTime(2025, 1, 20)
        };
        var claim = new Claim
        {
            CoverId = coverId,
            Created = new DateTime(2025, 1, 15).AddDays(dayOffset < 0 ? -10 : 10)
        };
        _coversRepository
            .Setup(repository => repository.GetByIdAsync(coverId))
            .ReturnsAsync(cover);

        var (createdClaim, error) = await _sut.CreateAsync(claim);

        Assert.Null(createdClaim);
        Assert.Equal("Claim created date must be within the related cover's insurance period.", error);
        _repository.Verify(repository => repository.AddAsync(It.IsAny<Claim>()), Times.Never);
        _auditService.Verify(audit => audit.AuditClaimAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenClaimDateIsWithinCoverPeriod_CreatesClaimAndAuditsCreation()
    {
        const string coverId = "cover-1";
        var cover = new Cover
        {
            StartDate = new DateTime(2025, 1, 10),
            EndDate = new DateTime(2025, 1, 20)
        };
        var claim = new Claim
        {
            CoverId = coverId,
            Created = new DateTime(2025, 1, 15),
            Name = "Test claim"
        };
        _coversRepository
            .Setup(repository => repository.GetByIdAsync(coverId))
            .ReturnsAsync(cover);

        var (createdClaim, error) = await _sut.CreateAsync(claim);

        Assert.Same(claim, createdClaim);
        Assert.Null(error);
        Assert.False(string.IsNullOrWhiteSpace(claim.Id));
        _repository.Verify(repository => repository.AddAsync(claim), Times.Once);
        _auditService.Verify(audit => audit.AuditClaimAsync(claim.Id, "POST"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenClaimDoesNotExist_ReturnsFalseAndAuditsDelete()
    {
        const string id = "missing-claim";
        _repository
            .Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync((Claim?)null);

        var result = await _sut.DeleteAsync(id);

        Assert.False(result);
        _auditService.Verify(audit => audit.AuditClaimAsync(id, "DELETE"), Times.Once);
        _repository.Verify(repository => repository.DeleteAsync(It.IsAny<Claim>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenClaimExists_DeletesClaimAndReturnsTrue()
    {
        const string id = "claim-1";
        var claim = new Claim { Id = id };
        _repository
            .Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync(claim);

        var result = await _sut.DeleteAsync(id);

        Assert.True(result);
        _auditService.Verify(audit => audit.AuditClaimAsync(id, "DELETE"), Times.Once);
        _repository.Verify(repository => repository.DeleteAsync(claim), Times.Once);
    }
}
