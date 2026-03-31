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
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? role)
        {
            var staff = await _staffService.GetAvailableStaffAsync(startDate, endDate, role);
            return Ok(staff);
        }
    }
}
