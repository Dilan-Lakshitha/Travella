using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.Interfaces;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int limit = 50)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var items = await _notificationService.GetForUserAsync(userId, Math.Clamp(limit, 1, 100));
            return Ok(items);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            await _notificationService.MarkAsReadAsync(id, userId);
            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { error = "Invalid user claim." });
            }

            await _notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var claim = User.FindFirst("userId")?.Value;
            return int.TryParse(claim, out userId) && userId > 0;
        }
    }
}
