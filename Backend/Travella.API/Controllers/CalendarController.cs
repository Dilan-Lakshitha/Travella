using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.Interfaces;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/admin/calendar")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;

        public CalendarController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        [HttpGet("staff-bookings")]
        public async Task<IActionResult> GetStaffBookings(
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] string? role = null)
        {
            if (!TryGetCompanyId(out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var items = await _calendarService.GetStaffBookingsAsync(companyId, startDate, endDate, role);
            return Ok(items);
        }

        [HttpGet("itinerary-bookings")]
        public async Task<IActionResult> GetItineraryBookings(
            [FromQuery] DateOnly? date,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] int? driverId = null,
            [FromQuery] int? guideId = null)
        {
            if (!TryGetCompanyId(out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var items = await _calendarService.GetItineraryBookingsAsync(
                companyId,
                date,
                startDate,
                endDate,
                driverId,
                guideId);

            return Ok(items);
        }

        private bool TryGetCompanyId(out int companyId)
        {
            companyId = 0;
            var claim = User.FindFirst("companyId")?.Value;
            return int.TryParse(claim, out companyId) && companyId > 0;
        }
    }
}
