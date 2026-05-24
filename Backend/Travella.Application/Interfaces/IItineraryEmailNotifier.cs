using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Application.Enums;
using Travella.Domain.Entities;

namespace Travella.Application.Interfaces
{
    public interface IItineraryEmailNotifier
    {
        Task NotifyAsync(Itinerary itinerary, ItineraryEmailEvent workflowEvent, ItineraryEmailContext? context = null);
    }
}
