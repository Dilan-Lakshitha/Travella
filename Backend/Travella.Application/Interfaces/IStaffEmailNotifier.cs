using System.Threading.Tasks;
using Travella.Domain.Entities;

namespace Travella.Application.Interfaces
{
    public interface IStaffEmailNotifier
    {
        Task NotifyDriverCreatedAsync(Staff driver, int companyId);

        Task NotifyGuideCreatedAsync(Staff guide, int companyId);

        Task NotifyStaffAssignedAsync(Itinerary itinerary, Staff driver, Staff guide, int companyId);

        Task NotifyItineraryConfirmedAsync(Itinerary itinerary, int companyId);

        Task NotifyItineraryStatusChangedAsync(Itinerary itinerary, int companyId, string statusLabel);
    }
}
