using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.DTOs;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/itinerary")]
    [Authorize(Roles = "STAFF,ADMIN")]
    public class PricingController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public PricingController(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        [HttpPost("pricing")]
        public async Task<IActionResult> Create([FromBody] ItineraryPricingInputDto request)
        {
            var createdBy = int.Parse(User.FindFirst("userId")!.Value);
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var pricingId = await _pricingService.CreatePricingAsync(request, createdBy, companyId);

            return Ok(new { pricingId });
        }

        [HttpPut("/api/pricing/update-margin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateMargin([FromBody] UpdatePricingMarginDto request)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            await _pricingService.UpdateMarginAsync(request, companyId);
            return Ok(new { message = "Profit margin updated." });
        }
    }
}
