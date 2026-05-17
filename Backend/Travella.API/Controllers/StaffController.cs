using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/staff")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableStaff(
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] string? role)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId) || companyId <= 0)
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var staff = await _staffService.GetAvailableStaffAsync(companyId, startDate, endDate, role);
            return Ok(staff);
        }
    }
}
