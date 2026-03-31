using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/pricing")]
    [Authorize(Roles = "STAFF")]
    public class PricingController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public PricingController(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePricingRequest request)
        {
            var createdBy = int.Parse(User.FindFirst("userId")!.Value);
            var pricingId = await _pricingService.CreatePricingAsync(
                request.ItineraryId,
                createdBy,
                request.TotalAmount
            );

            return Ok(new { pricingId });
        }
    }

    public class CreatePricingRequest
    {
        public int ItineraryId { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
