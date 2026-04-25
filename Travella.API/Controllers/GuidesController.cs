using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/guides")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public class GuidesController : ControllerBase
    {
        private readonly IStaffService _staffService;

        public GuidesController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetGuides()
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId) || companyId <= 0)
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var rows = await _staffService.GetGuidesAsync(companyId);
            return Ok(rows);
        }
    }
}

