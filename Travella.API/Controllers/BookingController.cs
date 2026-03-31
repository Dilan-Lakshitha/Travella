using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.DTOs;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/booking")]
    [Authorize(Roles = "TRAVELER")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] BookingDto dto)
        {
            var bookingId = await _bookingService.CreateBookingAsync(dto);
            return CreatedAtAction(nameof(CreateBooking), new { id = bookingId }, new { id = bookingId });
        }
    }
}
