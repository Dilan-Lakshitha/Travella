using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Application.DTOs;

namespace Travella.Application.Interfaces
{
    public interface INotificationRepository
    {
        Task<int> CreateAsync(int userId, int? itineraryId, string type, string title, string message);

        Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId, int limit);

        Task<int> GetUnreadCountAsync(int userId);

        Task<bool> MarkAsReadAsync(int notificationId, int userId);

        Task MarkAllAsReadAsync(int userId);
    }
}
