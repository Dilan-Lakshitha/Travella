using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Travella.API.Hubs;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;

namespace Travella.API.Services
{
    public class ItineraryChatNotifier : IItineraryChatNotifier
    {
        private readonly IHubContext<ItineraryChatHub> _hubContext;

        public ItineraryChatNotifier(IHubContext<ItineraryChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyMessageAsync(int itineraryId, ItineraryMessageDto message)
        {
            var groupName = $"itinerary-{itineraryId}";
            return _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", message);
        }
    }
}
