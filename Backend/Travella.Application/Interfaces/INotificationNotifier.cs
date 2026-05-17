using System.Threading.Tasks;
using Travella.Application.DTOs;

namespace Travella.Application.Interfaces
{
    public interface INotificationNotifier
    {
        Task NotifyUserAsync(int userId, NotificationDto notification, int unreadCount);

        Task NotifyUnreadCountAsync(int userId, int unreadCount);
    }
}
