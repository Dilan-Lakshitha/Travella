using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Travella.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userIdClaim = Context.User?.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out var userId) && userId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdClaim = Context.User?.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out var userId) && userId > 0)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId));
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static string UserGroup(int userId) => $"user-{userId}";
    }
}
