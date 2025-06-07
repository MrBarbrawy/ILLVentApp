using Microsoft.AspNetCore.SignalR;

namespace ILLVentApp.Application.Hubs
{
    public class EmergencyHub : Hub
    {
        public async Task JoinEmergencyTracking(int requestId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Emergency_{requestId}");
        }

        public async Task JoinUserTracking(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        public async Task JoinHospitalNotifications(int hospitalId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Hospital_{hospitalId}");
        }

        public async Task UpdateLocation(int requestId, double latitude, double longitude)
        {
            await Clients.Group($"Emergency_{requestId}")
                .SendAsync("LocationUpdated", new
                {
                    RequestId = requestId,
                    Latitude = latitude,
                    Longitude = longitude,
                    Timestamp = DateTime.UtcNow
                });
        }

        // Method for mobile app to join waiting for hospital response
        public async Task JoinEmergencyWaiting(int requestId, string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"EmergencyWaiting_{requestId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        // Method for mobile app to leave waiting (when hospital responds or request completes)
        public async Task LeaveEmergencyWaiting(int requestId, string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"EmergencyWaiting_{requestId}");
        }

        // Send heartbeat to maintain connection during waiting
        public async Task SendHeartbeat(int requestId)
        {
            await Clients.Group($"EmergencyWaiting_{requestId}")
                .SendAsync("Heartbeat", new
                {
                    RequestId = requestId,
                    Timestamp = DateTime.UtcNow,
                    Status = "Waiting"
                });
        }

        // Override connection events for better tracking
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
} 