// Hubs/NotificationHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace HospitalManagementAPI.Hubs
{
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public Task RegisterForUser(string userKey)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, userKey);
        }
    }
}
