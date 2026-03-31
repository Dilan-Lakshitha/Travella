using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/review")]
    [Authorize(Roles = "STAFF,ADMIN")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddReviewRequest request)
        {
            var reviewerId = int.Parse(User.FindFirst("userId")!.Value);
            var reviewerRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

            var reviewId = await _reviewService.AddReviewAsync(
                request.ItineraryId,
                reviewerId,
                reviewerRole,
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
        public string Status { get; set; } = "SUBMITTED";
    }
}
