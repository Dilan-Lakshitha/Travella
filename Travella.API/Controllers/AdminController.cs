using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("staff")]
        public async Task<IActionResult> GetStaffUsers()
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId) || companyId <= 0)
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var rows = await _adminService.GetCompanyStaffUsersAsync(companyId);
            return Ok(rows.Select(r => new { userId = r.UserId, name = r.Name, email = r.Email }));
        }

        [HttpPost("staff")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId) || companyId <= 0)
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var result = await _adminService.CreateStaffUserAsync(request.Name, request.Email, companyId);
            return Ok(new { email = result.Email, temporaryPassword = result.TemporaryPassword });
        }

        [HttpPost("drivers")]
        public async Task<IActionResult> CreateDriver([FromBody] CreateStaffResourceRequest request)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId) || companyId <= 0)
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var id = await _adminService.CreateDriverAsync(request.Name, request.Phone, request.Experience, request.Availability, companyId);
            return Ok(new { id });
        }

        [HttpPost("guides")]
        public async Task<IActionResult> CreateGuide([FromBody] CreateStaffResourceRequest request)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId) || companyId <= 0)
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var id = await _adminService.CreateGuideAsync(request.Name, request.Phone, request.Experience, request.Availability, companyId);
            return Ok(new { id });
        }
    }

    public class CreateStaffRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CreateStaffResourceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Experience { get; set; }
        public string Availability { get; set; } = "AVAILABLE";
    }
}

