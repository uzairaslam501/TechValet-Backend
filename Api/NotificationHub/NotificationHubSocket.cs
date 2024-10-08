using Microsoft.AspNetCore.SignalR;

namespace ITValet.NotificationHub
{
    public class NotificationHubSocket : Hub
    {
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", senderId, receiverId, message);
        }


    }
}
