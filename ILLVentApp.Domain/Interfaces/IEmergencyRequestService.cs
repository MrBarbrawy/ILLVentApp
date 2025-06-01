using ILLVentApp.Domain.DTOs;

namespace ILLVentApp.Domain.Interfaces
{
    /// <summary>
    /// Interface for emergency request service operations
    /// Handles emergency requests, hospital notifications, and real-time tracking
    /// </summary>
    public interface IEmergencyRequestService
    {
        /// <summary>
        /// Creates a new emergency request with location tracking and hospital notifications
        /// </summary>
        /// <param name="userId">The ID of the user making the emergency request</param>
        /// <param name="request">Emergency request details including location and optional medical history</param>
        /// <returns>Emergency request response with nearby hospitals and tracking information</returns>
        Task<EmergencyRequestResponseDto> CreateEmergencyRequestAsync(string userId, CreateEmergencyRequestDto request);

        /// <summary>
        /// Gets detailed emergency request information for hospital review
        /// </summary>
        /// <param name="requestId">The emergency request ID</param>
        /// <param name="hospitalId">The hospital ID requesting the details</param>
        /// <returns>Detailed emergency request information including patient data and medical history</returns>
        Task<EmergencyRequestDetailsDto> GetEmergencyRequestDetailsAsync(int requestId, int hospitalId);

        /// <summary>
        /// Hospital responds to an emergency request (accept or reject)
        /// </summary>
        /// <param name="response">Hospital response including acceptance status and details</param>
        /// <returns>True if the response was processed successfully</returns>
        Task<bool> RespondToEmergencyRequestAsync(HospitalEmergencyResponseDto response);

        /// <summary>
        /// Updates the user's location during an active emergency for real-time tracking
        /// </summary>
        /// <param name="locationUpdate">New location coordinates and timestamp</param>
        /// <returns>True if the location was updated successfully</returns>
        Task<bool> UpdateEmergencyLocationAsync(LocationUpdateDto locationUpdate);

        /// <summary>
        /// Gets active emergency requests for a specific hospital within a given radius
        /// </summary>
        /// <param name="hospitalId">The hospital ID</param>
        /// <param name="radiusKm">Search radius in kilometers (default: 20km)</param>
        /// <returns>List of active emergency requests within the specified radius</returns>
        Task<List<EmergencyRequestDetailsDto>> GetActiveEmergencyRequestsForHospitalAsync(int hospitalId, double radiusKm = 20.0);

        /// <summary>
        /// Completes/closes an emergency request when resolved
        /// </summary>
        /// <param name="requestId">The emergency request ID to complete</param>
        /// <param name="userId">The user ID confirming completion</param>
        /// <returns>True if the request was completed successfully</returns>
        Task<bool> CompleteEmergencyRequestAsync(int requestId, string userId);

        /// <summary>
        /// Gets the user's currently active emergency request
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Active emergency request details, or null if no active request</returns>
        Task<EmergencyRequestDetailsDto> GetUserActiveEmergencyRequestAsync(string userId);
    }
} 