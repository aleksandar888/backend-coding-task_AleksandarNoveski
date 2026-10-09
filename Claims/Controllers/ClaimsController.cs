using Claims.Models;
using Microsoft.AspNetCore.Mvc;
using Claims.Services.Logging;
using Claims.Services.Claims;

namespace Claims.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClaimsController : ControllerBase
    {
        private readonly IClaimsService _claimsService;
        private readonly IErrorLoggingService _errorLoggingService;

        public ClaimsController(IClaimsService claimsService, IErrorLoggingService errorLoggingService)
        {
            _claimsService = claimsService;
            _errorLoggingService = errorLoggingService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Claim>>> GetAsync()
        {
            try
            {
                var result = await _claimsService.GetAllAsync();
                return Ok(result);
            }
            catch (Exception exception)
            {
                await _errorLoggingService.LogErrorAsync(
                    exception,
                    "An error occurred while retrieving all claims.",
                    nameof(ClaimsController),
                    nameof(GetAsync),
                    HttpContext.TraceIdentifier);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred while retrieving claims. Please try again later.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateAsync(CreateClaimRequest request)
        {
            try
            {
                var claim = new Claim()
                {
                    CoverId = request.CoverId,
                    Name = request.Name,
                    Type = request.Type,
                    DamageCost = request.DamageCost,
                    Created = DateTime.Now
                };

                var (createdClaim, error) = await _claimsService.CreateAsync(claim);
                if (error is not null)
                {
                    return BadRequest(error);
                }

                return CreatedAtRoute("GetClaimById", new { id = createdClaim?.Id }, createdClaim);
            }
            catch (Exception exception)
            {
                await _errorLoggingService.LogErrorAsync(
                    exception,
                    "An error occurred while creating a claim.",
                    nameof(ClaimsController),
                    nameof(CreateAsync),
                    HttpContext.TraceIdentifier);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred while creating the claim. Please try again later.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            try
            {
                var deleted = await _claimsService.DeleteAsync(id);

                return deleted
                    ? NoContent()
                    : NotFound($"Claim with ID '{id}' was not found."); ;
            }
            catch (Exception exception)
            {
                await _errorLoggingService.LogErrorAsync(
                    exception,
                    "An error occurred while deleting a claim.",
                    nameof(ClaimsController),
                    nameof(DeleteAsync),
                    HttpContext.TraceIdentifier);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred while deleting the claim. Please try again later.");
            }
        }

        [HttpGet("{id}", Name = "GetClaimById")]
        public async Task<ActionResult<Claim>> GetAsync(string id)
        {
            try
            {
                var claim = await _claimsService.GetByIdAsync(id);

                return claim is null
                    ? NotFound($"Claim with ID '{id}' was not found.")
                    : Ok(claim);
            }
            catch (Exception exception)
            {
                await _errorLoggingService.LogErrorAsync(
                    exception,
                    "An error occurred while retrieving a claim by ID.",
                    nameof(ClaimsController),
                    nameof(GetAsync),
                    HttpContext.TraceIdentifier);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred while retrieving the claim. Please try again later.");
            }
        }
    }
}
