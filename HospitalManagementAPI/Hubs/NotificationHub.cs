// Hubs/NotificationHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace HospitalManagementAPI.Hubs
{
    // Simple hub — we will send messages by user or group from server via IHubContext
    public class NotificationHub : Hub
    {
        // Optional: override connect/disconnect for logging
        public override Task OnConnectedAsync()
        {
            // client can send their user identifier after connecting (see client code below)
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        // Optionally allow client to register themselves to groups (eg: "doctor-{id}", "patient-{id}")
        public Task RegisterForUser(string userKey)
        {
            // join a group by userKey for easy server targeting
            return Groups.AddToGroupAsync(Context.ConnectionId, userKey);
        }
    }
}
