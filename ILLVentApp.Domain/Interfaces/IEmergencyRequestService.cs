using ILLVentApp.Domain.DTOs;

namespace ILLVentApp.Domain.Interfaces
{
 

    public interface IEmergencyRequestService
    {
     
        Task<EmergencyRequestResponseDto> CreateEmergencyRequestAsync(string userId, CreateEmergencyRequestDto request);

    
        Task<EmergencyRequestDetailsDto> GetEmergencyRequestDetailsAsync(int requestId, int hospitalId);

      
        Task<bool> RespondToEmergencyRequestAsync(HospitalEmergencyResponseDto response);

     
        Task<bool> UpdateEmergencyLocationAsync(LocationUpdateDto locationUpdate);

    
        Task<List<EmergencyRequestDetailsDto>> GetActiveEmergencyRequestsForHospitalAsync(int hospitalId, double radiusKm = 20.0);

 
        Task<bool> CompleteEmergencyRequestAsync(int requestId, string userId);

     
        Task<EmergencyRequestDetailsDto> GetUserActiveEmergencyRequestAsync(string userId);
    }
} 