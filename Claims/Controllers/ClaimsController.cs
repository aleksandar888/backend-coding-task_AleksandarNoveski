using Claims.Models;
using Claims.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Claims.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClaimsController : ControllerBase
    {
        private readonly IClaimsService _claimsService;

        public ClaimsController(IClaimsService claimsService)
        {
            _claimsService = claimsService;
        }

        [HttpGet]
        public async Task<IEnumerable<Claim>> GetAsync()
        {
            return await _claimsService.GetAllAsync();
        }

        [HttpPost]
        public async Task<ActionResult> CreateAsync(Claim claim)
        {
            try
            {
                return Ok(await _claimsService.CreateAsync(claim));
            }
            catch (ValidationException exception)
            {
                return BadRequest(exception.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task DeleteAsync(string id)
        {
            await _claimsService.DeleteAsync(id);
        }

        [HttpGet("{id}")]
        public async Task<Claim?> GetAsync(string id)
        {
            return await _claimsService.GetByIdAsync(id);
        }
    }
}
