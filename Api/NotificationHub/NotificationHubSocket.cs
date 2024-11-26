using ITValet.HelpingClasses;
using Microsoft.AspNetCore.SignalR;

namespace ITValet.NotificationHub
{
    public class NotificationHubSocket : Hub
    {
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", senderId, receiverId, message);
        }

        public async Task SendOfferObject(string senderId, string receiverId, ViewModelMessageChatBox obj)
        {
            await Clients.All.SendAsync("ReceiveOffers", senderId, receiverId, obj);
        }
    }
}
