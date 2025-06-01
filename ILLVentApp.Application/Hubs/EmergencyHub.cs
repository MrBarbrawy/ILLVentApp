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
    }
} 