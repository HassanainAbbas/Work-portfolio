using Microsoft.AspNetCore.SignalR;

namespace Organify.Hubs
{
    public class Notifications : Hub
    {
        public async Task SendMessage(String message)
        {
            await Clients.Others.SendAsync("ReceiveMessage", message);
        }
    }
}
