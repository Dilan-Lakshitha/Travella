using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Travella.API.Hubs;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;

namespace Travella.API.Services
{
    public class NotificationNotifier : INotificationNotifier
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationNotifier(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyUserAsync(int userId, NotificationDto notification, int unreadCount)
        {
            var group = NotificationHub.UserGroup(userId);
            await _hubContext.Clients.Group(group).SendAsync("ReceiveNotification", notification);
            await _hubContext.Clients.Group(group).SendAsync("UnreadCountChanged", unreadCount);
        }

        public Task NotifyUnreadCountAsync(int userId, int unreadCount)
        {
            var group = NotificationHub.UserGroup(userId);
            return _hubContext.Clients.Group(group).SendAsync("UnreadCountChanged", unreadCount);
        }
    }
}
