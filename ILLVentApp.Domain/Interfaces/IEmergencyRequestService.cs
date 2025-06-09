using ILLVentApp.Domain.DTOs;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IEmergencyRequestService
    {
        // Updated to use comprehensive error handling
        Task<EmergencyResult> CreateEmergencyRequestAsync(string userId, CreateEmergencyRequestDto request);

        // Emergency request details for hospital dashboard
        Task<EmergencyRequestDetailsDto> GetEmergencyRequestDetailsAsync(int requestId, int hospitalId);

        // Updated hospital response handling
        Task<HospitalResponseResult> RespondToEmergencyRequestAsync(HospitalEmergencyResponseDto response);

        // Updated location tracking
        Task<LocationUpdateResult> UpdateEmergencyLocationAsync(LocationUpdateDto locationUpdate);

        // Hospital dashboard - active requests
        Task<List<EmergencyRequestDetailsDto>> GetActiveEmergencyRequestsForHospitalAsync(int hospitalId, double radiusKm = 20.0);

        // Complete emergency request
        Task<bool> CompleteEmergencyRequestAsync(int requestId, string userId);

        // Get user's active emergency request
        Task<EmergencyRequestDetailsDto> GetUserActiveEmergencyRequestAsync(string userId);
    }
} 