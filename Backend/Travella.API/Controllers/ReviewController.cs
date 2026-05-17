using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/itinerary")]
    [Authorize(Roles = "STAFF,ADMIN")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost("review")]
        public async Task<IActionResult> Add([FromBody] AddReviewRequest request)
        {
            var reviewerId = int.Parse(User.FindFirst("userId")!.Value);
            var reviewerRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var reviewId = await _reviewService.AddReviewAsync(
                request.ItineraryId,
                reviewerId,
                reviewerRole,
                companyId,
                request.Comments,
                request.Status
            );

            return Ok(new { reviewId });
        }
    }

    public class AddReviewRequest
    {
        public int ItineraryId { get; set; }
        public string Comments { get; set; } = string.Empty;
        // Supported values (drives workflow + inserted into tbl_itinerary_reviews.status):
        // PENDING
        // REQUESTED_CHANGES
        // APPROVED_BY_STAFF
        // APPROVED_BY_ADMIN
        // REJECTED
        public string Status { get; set; } = "PENDING";
    }
}
