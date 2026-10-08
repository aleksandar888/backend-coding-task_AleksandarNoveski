using Claims.Models;
using Claims.Contracts.Requests;
using Claims.Logging;
using Claims.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController : ControllerBase
{
    private readonly ICoversService _coversService;
    private readonly IErrorLoggingService _errorLoggingService;

    public CoversController(
        ICoversService coversService,
        IErrorLoggingService errorLoggingService)
    {
        _coversService = coversService;
        _errorLoggingService = errorLoggingService;
    }

    [HttpPost("compute")]
    public ActionResult ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        return Ok(_coversService.ComputePremium(startDate, endDate, coverType));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync()
    {
        var results = await _coversService.GetAllAsync();
        return Ok(results);
    }

    [HttpGet("{id}", Name = "GetCoverById")]
    public async Task<ActionResult<Cover>> GetAsync(string id)
    {
        return Ok(await _coversService.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult> CreateAsync(CreateCoverRequest request)
    {
        try
        {
            var cover = new Cover
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Type = request.Type
            };

            var createdCover = await _coversService.CreateAsync(cover);

            if (createdCover.Id is null)
            {
                return BadRequest("The cover was not created. Please check the provided details and try again.");
            }

            return CreatedAtRoute("GetCoverById", new { id = createdCover.Id }, createdCover);
        }
        catch (Exception exception)
        {
            await _errorLoggingService.LogErrorAsync(
                exception,
                "An error occurred while creating a cover.",
                nameof(CoversController),
                nameof(CreateAsync),
                HttpContext.TraceIdentifier);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "The cover could not be created due to an unexpected error. Please try again later.");
        }
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(string id)
    {
        await _coversService.DeleteAsync(id);
    }
}
