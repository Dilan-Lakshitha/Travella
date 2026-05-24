using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Travella.API.Hubs
{
    [Authorize]
    public class ItineraryChatHub : Hub
    {
        /// <summary>
        /// Join an itinerary chat group
        /// </summary>
        public async Task JoinItineraryChat(int itineraryId)
        {
            var groupName = $"itinerary-{itineraryId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Leave an itinerary chat group
        /// </summary>
        public async Task LeaveItineraryChat(int itineraryId)
        {
            var groupName = $"itinerary-{itineraryId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Send a message to all connected users in the itinerary chat
        /// </summary>
        public async Task SendMessage(int itineraryId, int senderId, string senderName, string senderRole, string message)
        {
            var groupName = $"itinerary-{itineraryId}";
            
            var messageData = new
            {
                senderId,
                senderName,
                senderRole,
                message,
                timestamp = DateTime.UtcNow
            };

            await Clients.Group(groupName).SendAsync("ReceiveMessage", messageData);
        }

        public async Task NotifyTyping(int itineraryId, int senderId, string senderRole)
        {
            var groupName = $"itinerary-{itineraryId}";

            await Clients.GroupExcept(groupName, Context.ConnectionId)
                .SendAsync("UserTyping", new
                {
                    itineraryId,
                    senderId,
                    senderRole
                });
        }

        public async Task NotifyStoppedTyping(int itineraryId, int senderId)
        {
            var groupName = $"itinerary-{itineraryId}";

            await Clients.GroupExcept(groupName, Context.ConnectionId)
                .SendAsync("UserStoppedTyping", new
                {
                    itineraryId,
                    senderId
                });
        }
    }
}
