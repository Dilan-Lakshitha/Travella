using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Travella.Application.DTOs;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/drivers")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public class DriversController : ControllerBase
    {
        private readonly IStaffService _staffService;

        public DriversController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDrivers()
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId) || companyId <= 0)
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var rows = await _staffService.GetDriversAsync(companyId);
            return Ok(rows.Select(StaffResourceMapper.ToDto));
        }
    }
}

