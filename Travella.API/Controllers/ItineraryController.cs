using System;
using System.Security.Claims;
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

        [HttpPost("save-from-google")]
        [Authorize(Roles = "TRAVELER,STAFF,ADMIN")]
        public async Task<IActionResult> SaveFromGoogle([FromBody] SaveGoogleAttractionDto dto)
        {
            var result = await _itineraryService.SaveGoogleAttractionAsync(dto);
            return Ok(new { id = result.Id, alreadyExists = result.AlreadyExists });
        }

        [HttpPost]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> CreateItinerary([FromBody] ItineraryDraftUpsertDto dto)
        {
            var guestIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(guestIdClaim, out var guestId) || guestId <= 0)
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var companyIdClaim = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId) || companyId <= 0)
            {
                return BadRequest(new { error = "Company id is required on your account to create an itinerary." });
            }

            try
            {
                var itineraryId = await _itineraryService.CreateItineraryAsync(dto, guestId, companyId);
                return CreatedAtAction(nameof(GetItineraryById), new { id = itineraryId }, new { id = itineraryId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> UpdateItinerary(int id, [FromBody] ItineraryDraftUpsertDto dto)
        {
            var travelerIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(travelerIdValue, out var travelerId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            try
            {
                await _itineraryService.SaveItineraryDraftAsync(id, dto, travelerId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> DeleteDraft(int id)
        {
            var travelerIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(travelerIdValue, out var travelerId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            await _itineraryService.DeleteDraftItineraryAsync(id, travelerId);
            return NoContent();
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetItineraryById(int id)
        {
            var userIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            int? companyId = int.TryParse(User.FindFirst("companyId")?.Value, out var cid) ? cid : null;

            var itinerary = await _itineraryService.GetItineraryFullAsync(id, userId, role, companyId);
            if (itinerary == null)
            {
                return NotFound();
            }

            return Ok(itinerary);
        }

        [HttpPost("{id:int}/days")]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> AddDay(int id, [FromBody] AddItineraryDayDto dto)
        {
            dto.ItineraryId = id;
            var travelerIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(travelerIdValue, out var travelerId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var dayId = await _itineraryService.AddDayAsync(dto, travelerId);
            return Ok(new { dayId });
        }

        [HttpPost("{id:int}/attractions")]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> AddAttraction(int id, [FromBody] AddAttractionDto dto)
        {
            _ = id;
            var travelerIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(travelerIdValue, out var travelerId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            await _itineraryService.AddAttractionAsync(dto, travelerId);
            return NoContent();
        }

        [HttpPost("{id:int}/accommodations")]
        public async Task<IActionResult> AssignAccommodation(int id, [FromBody] AssignAccommodationDto dto)
        {
            _ = id;
            await _itineraryService.AssignAccommodationAsync(dto);
            return NoContent();
        }

        [HttpPost("day")]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> AddDayFlat([FromBody] AddItineraryDayDto dto)
        {
            var travelerIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(travelerIdValue, out var travelerId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var dayId = await _itineraryService.AddDayAsync(dto, travelerId);
            return Ok(new { dayId });
        }

        [HttpPost("attraction")]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> AddAttractionFlat([FromBody] AddAttractionDto dto)
        {
            var travelerIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(travelerIdValue, out var travelerId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            await _itineraryService.AddAttractionAsync(dto, travelerId);
            return NoContent();
        }

        [HttpGet("/api/guest/itineraries")]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> GetGuestItineraries()
        {
            var userIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var rows = await _itineraryService.GetGuestItinerariesAsync(userId);
            return Ok(rows);
        }

        [HttpGet("/api/agency/review-itineraries")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> GetReviewItineraries()
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var rows = await _itineraryService.GetSubmittedItinerariesAsync(companyId);
            return Ok(rows);
        }

        [HttpPost("{id:int}/start-review")]
        [Authorize(Roles = "STAFF")]
        public async Task<IActionResult> StartReview(int id)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            await _itineraryService.MarkUnderReviewAsync(id, companyId);
            return Ok(new { message = "Itinerary moved to under_review." });
        }

        [HttpGet("/api/company/itineraries")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetCompanyItineraries()
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var rows = await _itineraryService.GetCompanyItinerariesAsync(companyId);
            return Ok(rows);
        }

        [HttpPost("{id:int}/submit")]
        [Authorize(Roles = "TRAVELER")]
        public async Task<IActionResult> SubmitItineraryById(int id)
        {
            var travelerIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(travelerIdValue, out var travelerId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            try
            {
                await _itineraryService.SubmitItineraryAsync(id, travelerId);
                return Ok(new { message = "Itinerary submitted." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/api/owner/submitted-itineraries")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetOwnerSubmittedItineraries()
        {
            var rows = await _itineraryService.GetOwnerSubmittedItinerariesAsync();
            return Ok(rows);
        }

        [HttpPost("approve")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> Approve([FromBody] UpdateItineraryStatusDto dto)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var approverRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(approverRole))
            {
                return BadRequest(new { error = "Approver role claim is missing." });
            }

            await _itineraryService.ApproveItineraryAsync(dto.ItineraryId, approverRole, companyId);
            return Ok(new { message = "Itinerary approved." });
        }

        [HttpPost("reject")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> Reject([FromBody] UpdateItineraryStatusDto dto)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            await _itineraryService.RejectItineraryAsync(dto.ItineraryId, companyId);
            return Ok(new { message = "Itinerary rejected." });
        }

        [HttpPost("{id:int}/request-correction")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> RequestCorrection(int id, [FromBody] AddItineraryMessageDto dto)
        {
            var senderIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(senderIdValue, out var senderId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            var senderRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "STAFF";
            await _itineraryService.RequestCorrectionAsync(id, senderId, senderRole, companyId, dto.Message);
            return Ok(new { message = "Returned for correction." });
        }

        [HttpPost("confirm")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Confirm([FromBody] UpdateItineraryStatusDto dto)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            await _itineraryService.ConfirmItineraryAsync(dto.ItineraryId, companyId);
            return Ok(new { message = "Itinerary confirmed." });
        }

        [HttpPost("assign")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> AssignDriverGuide([FromBody] AssignItineraryStaffDto request)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            await _itineraryService.AssignDriverGuideAsync(request.ItineraryId, request.DriverId, request.GuideId, companyId);
            return Ok(new { message = "Driver and guide assigned." });
        }

        [HttpPost("{id:int}/send-to-admin")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> SendToAdmin(int id)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            await _itineraryService.SendToAdminAsync(id, companyId);
            return Ok(new { message = "Sent to admin." });
        }

        [HttpPost("{id:int}/assign-staff")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> AssignStaff(int id, [FromBody] AssignStaffDto dto)
        {
            dto.ItineraryId = id;
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            await _itineraryService.AssignStaffAsync(dto, companyId);
            return NoContent();
        }

        [HttpPost("admin-approve")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AdminApprove([FromBody] UpdateItineraryStatusDto dto)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            await _itineraryService.ApproveItineraryAsync(dto.ItineraryId, "ADMIN", companyId);
            return Ok(new { message = "Itinerary approved by owner." });
        }

        [HttpPost("admin-reject")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AdminReject([FromBody] UpdateItineraryStatusDto dto)
        {
            var companyIdValue = User.FindFirst("companyId")?.Value;
            if (!int.TryParse(companyIdValue, out var companyId))
            {
                return BadRequest(new { error = "Company id claim is required." });
            }

            await _itineraryService.RejectItineraryAsync(dto.ItineraryId, companyId);
            return Ok(new { message = "Itinerary rejected by owner." });
        }

        [HttpGet("{id:int}/messages")]
        [Authorize]
        public async Task<IActionResult> GetMessages(int id)
        {
            var userIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            int? companyId = int.TryParse(User.FindFirst("companyId")?.Value, out var cid) ? cid : null;
            var rows = await _itineraryService.GetItineraryMessagesAsync(id, userId, role, companyId);
            return Ok(rows);
        }

        [HttpPost("{id:int}/messages")]
        [Authorize]
        public async Task<IActionResult> AddMessage(int id, [FromBody] AddItineraryMessageDto dto)
        {
            var userIdValue = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            int? companyId = int.TryParse(User.FindFirst("companyId")?.Value, out var cid) ? cid : null;
            var messageId = await _itineraryService.AddItineraryMessageAsync(id, userId, role, companyId, dto);
            return Ok(new { id = messageId });
        }
    }
}
