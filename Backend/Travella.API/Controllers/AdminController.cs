using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Travella.Application.DTOs;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IItineraryService _itineraryService;

        public AdminController(IAdminService adminService, IItineraryService itineraryService)
        {
            _adminService = adminService;
            _itineraryService = itineraryService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId) || companyId <= 0)
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var dashboard = await _itineraryService.GetAdminDashboardAsync(companyId);
            return Ok(dashboard);
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

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                return BadRequest(new { error = "Phone is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                return BadRequest(new { error = "Language is required." });
            }

            var id = await _adminService.CreateDriverAsync(
                request.Name,
                request.Phone,
                request.Experience,
                request.Availability,
                request.Language,
                request.Email,
                companyId);
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

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                return BadRequest(new { error = "Phone is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                return BadRequest(new { error = "Language is required." });
            }

            var id = await _adminService.CreateGuideAsync(
                request.Name,
                request.Phone,
                request.Experience,
                request.Availability,
                request.Language,
                request.Email,
                companyId);
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
        public string Language { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}

