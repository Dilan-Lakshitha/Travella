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

            var pricing = await _pricingService.CreatePricingAsync(request, createdBy, companyId);
            return Ok(pricing);
        }

        [HttpGet("{itineraryId:int}/pricing")]
        public async Task<IActionResult> GetPricing(int itineraryId)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var pricing = await _pricingService.GetPricingForItineraryAsync(itineraryId, companyId);
            if (pricing == null)
            {
                return NotFound();
            }

            return Ok(pricing);
        }
    }

    [ApiController]
    [Route("api/pricing")]
    [Authorize(Roles = "ADMIN")]
    public class PricingAdminController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public PricingAdminController(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        [HttpPut("update-margin")]
        public async Task<IActionResult> UpdateMargin([FromBody] UpdatePricingMarginDto request)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var pricing = await _pricingService.UpdateMarginAsync(request, companyId);
            return Ok(pricing);
        }
    }
}
