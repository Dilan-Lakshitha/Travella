using System.Threading.Tasks;
using Travella.Application.DTOs;

namespace Travella.Application.Interfaces
{
    public interface IItineraryChatNotifier
    {
        Task NotifyMessageAsync(int itineraryId, ItineraryMessageDto message);
    }
}
