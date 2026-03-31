using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.DTOs;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/itinerary")]
    [Authorize]
    public class ItineraryController : ControllerBase
    {
        private readonly IItineraryService _itineraryService;

        public ItineraryController(IItineraryService itineraryService)
        {
            _itineraryService = itineraryService;
        }

        [HttpPost]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> CreateItinerary([FromBody] CreateItineraryDto dto)
        {
            var itineraryId = await _itineraryService.CreateItineraryAsync(dto);
            return CreatedAtAction(nameof(GetItineraryById), new { id = itineraryId }, new { id = itineraryId });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetItineraryById(int id)
        {
            var itinerary = await _itineraryService.GetItineraryAsync(id);
            if (itinerary == null)
            {
                return NotFound();
            }

            return Ok(itinerary);
        }

        [HttpPost("{id:int}/days")]
        public async Task<IActionResult> AddDay(int id, [FromBody] AddItineraryDayDto dto)
        {
            dto.ItineraryId = id;
            await _itineraryService.AddDayAsync(dto);
            return NoContent();
        }

        [HttpPost("{id:int}/attractions")]
        public async Task<IActionResult> AddAttraction(int id, [FromBody] AddAttractionDto dto)
        {
            _ = id;
            await _itineraryService.AddAttractionAsync(dto);
            return NoContent();
        }

        [HttpPost("{id:int}/accommodations")]
        public async Task<IActionResult> AssignAccommodation(int id, [FromBody] AssignAccommodationDto dto)
        {
            _ = id;
            await _itineraryService.AssignAccommodationAsync(dto);
            return NoContent();
        }

        [HttpPost("{id:int}/assign-staff")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AssignStaff(int id, [FromBody] AssignStaffDto dto)
        {
            dto.ItineraryId = id;
            await _itineraryService.AssignStaffAsync(dto);
            return NoContent();
        }
    }
}
