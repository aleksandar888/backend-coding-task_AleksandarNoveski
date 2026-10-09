using Claims.Models;
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
        try
        {
            if (startDate == default || endDate == default || startDate >= endDate || !Enum.IsDefined(typeof(CoverType), coverType))
            {
                return BadRequest("The provided parameters are invalid. Please provide valid start and end dates, ensure the start date is earlier than the end date, and specify a valid cover type.");
            }

            return Ok(_coversService.ComputePremium(startDate, endDate, coverType));
        }
        catch (Exception exception)
        {
            _errorLoggingService.LogErrorAsync(
               exception,
               "An error occurred while computing the cover premium.",
               nameof(CoversController),
               nameof(ComputePremium),
               HttpContext.TraceIdentifier);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred while computing the cover premium. Please try again later.");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync()
    {
        try
        {
            var results = await _coversService.GetAllAsync();
            return Ok(results);
        }
        catch (Exception exception)
        {
            await _errorLoggingService.LogErrorAsync(
               exception,
               "An error occurred while retrieving all covers.",
               nameof(CoversController),
               nameof(GetAsync),
               HttpContext.TraceIdentifier);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred while retrieving covers. Please try again later.");
        }
    }

    [HttpGet("{id}", Name = "GetCoverById")]
    public async Task<ActionResult<Cover>> GetAsync(string id)
    {
        try
        {
            var cover = await _coversService.GetByIdAsync(id);

            return cover is null
                ? NotFound($"Cover with ID '{id}' was not found.")
                : Ok(cover);
        }
        catch (Exception exception)
        {
            await _errorLoggingService.LogErrorAsync(
               exception,
               "An error occurred while retrieving a cover by ID.",
               nameof(CoversController),
               nameof(GetAsync),
               HttpContext.TraceIdentifier);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred while retrieving the cover. Please try again later.");
        }
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
                "An unexpected error occurred while creating the cover. Please try again later.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        try
        {
            var deleted = await _coversService.DeleteAsync(id);

            return deleted
                ? NoContent()
                : NotFound($"Cover with ID '{id}' was not found.");
        }
        catch (Exception exception)
        {
            await _errorLoggingService.LogErrorAsync(
                exception,
                "An error occurred while deleting a cover.",
                nameof(CoversController),
                nameof(DeleteAsync),
                HttpContext.TraceIdentifier);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An error occurred while deleting the cover.");
        }
    }
}
