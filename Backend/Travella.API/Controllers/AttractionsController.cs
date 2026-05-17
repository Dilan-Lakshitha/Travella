using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.DTOs;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/attractions")]
    [Authorize]
    public class AttractionsController : ControllerBase
    {
        private readonly IItineraryService _itineraryService;

        public AttractionsController(IItineraryService itineraryService)
        {
            _itineraryService = itineraryService;
        }

        [HttpPost("google")]
        [Authorize(Roles = "TRAVELER,STAFF,ADMIN")]
        public async Task<IActionResult> SaveGoogleAttraction([FromBody] SaveGoogleAttractionDto dto)
        {
            var result = await _itineraryService.SaveGoogleAttractionAsync(dto);
            return Ok(new
            {
                id = result.Id,
                alreadyExists = result.AlreadyExists
            });
        }
    }
}
