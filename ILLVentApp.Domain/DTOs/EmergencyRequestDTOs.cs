using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ILLVentApp.Domain.DTOs
{
    // DTO for creating a new emergency request
    public class CreateEmergencyRequestDto
    {
        public string? InjuryDescription { get; set; } // Can be null
        
        [AllowNull]
        public string? InjuryPhotoBase64 { get; set; } // Can be null - explicitly marked as optional
        
        // Medical history access (both can be null)
        [AllowNull]
        public string? MedicalHistoryQrCode { get; set; } // Can be null - explicitly marked as optional
        
        [AllowNull]
        public string? MedicalHistoryToken { get; set; } // Can be null - explicitly marked as optional
        
        // Location coordinates (optional - will use Cairo defaults if not provided)
        public double? UserLatitude { get; set; } // Optional - defaults to Cairo: 30.0618
        public double? UserLongitude { get; set; } // Optional - defaults to Cairo: 31.2186
        
        public int? Priority { get; set; } // Priority level (1=High, 2=Medium, 3=Low)
    }

    // DTO for emergency request response
    public class EmergencyRequestResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? RequestId { get; set; }
        public List<NearbyHospitalDto> NearbyHospitals { get; set; } = new();
        public string TrackingId { get; set; } // For SignalR tracking
    }

    // DTO for nearby hospital information
    public class NearbyHospitalDto
    {
        public int HospitalId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public double DistanceKm { get; set; }
        public bool IsAvailable { get; set; }
        public string ContactNumber { get; set; }
        public List<string> Specialties { get; set; } = new();
    }

    // DTO for hospital response to emergency
    public class HospitalEmergencyResponseDto
    {
        [Required]
        public int RequestId { get; set; }
        
        [Required]
        public int HospitalId { get; set; }
        
        [Required]
        public bool IsAccepted { get; set; }
        
        public string ResponseMessage { get; set; }
        
        // Optional fields - only required for acceptance
        public int? EstimatedResponseTimeMinutes { get; set; } // Only required if IsAccepted = true
        public bool AmbulanceAvailable { get; set; } = false;
    }

    // DTO for real-time location updates
    public class LocationUpdateDto
    {
        [Required]
        public int RequestId { get; set; }
        
        [Required]
        public double Latitude { get; set; }
        
        [Required]
        public double Longitude { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // DTO for emergency request details (for hospital dashboard)
    public class EmergencyRequestDetailsDto
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; }
        public string InjuryDescription { get; set; }
        public string InjuryPhotoUrl { get; set; }
        public double UserLatitude { get; set; }
        public double UserLongitude { get; set; }
        public double DistanceFromHospital { get; set; }
        public DateTime Timestamp { get; set; }
        public int Priority { get; set; }
        
        // Medical history (emergency subset)
        public EmergencyMedicalHistoryDto MedicalHistory { get; set; }
    }

    // DTO for emergency medical history (limited data for emergency personnel)
    public class EmergencyMedicalHistoryDto
    {
        // Basic patient information
        [JsonPropertyName("age")]
        public int Age { get; set; }
        
        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }
        
        [JsonPropertyName("height")]
        public decimal Height { get; set; }
        
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
        
        // Blood and medical conditions
        [JsonPropertyName("bloodType")]
        public string BloodType { get; set; }
        
        [JsonPropertyName("hasAllergies")]
        public bool HasAllergies { get; set; }
        
        [JsonPropertyName("allergiesDetails")]
        public string AllergiesDetails { get; set; }
        
        [JsonPropertyName("medicalConditions")]
        public List<string> MedicalConditions { get; set; } = new();
        
        [JsonPropertyName("hasBloodTransfusionObjection")]
        public bool HasBloodTransfusionObjection { get; set; }
        
        [JsonPropertyName("hasDiabetes")]
        public bool HasDiabetes { get; set; }
        
        [JsonPropertyName("diabetesType")]
        public string DiabetesType { get; set; }
        
        [JsonPropertyName("hasHighBloodPressure")]
        public bool HasHighBloodPressure { get; set; }
        
        [JsonPropertyName("hasLowBloodPressure")]
        public bool HasLowBloodPressure { get; set; }
        
        // Surgical history
        [JsonPropertyName("hasSurgeryHistory")]
        public bool HasSurgeryHistory { get; set; }
        
        [JsonPropertyName("surgicalHistories")]
        public List<EmergencySurgicalHistoryDto> SurgicalHistories { get; set; } = new();
        
        public string DataSource { get; set; } // "QRCode", "Token", "None"
    }

    // DTO for surgical history in emergency context
    public class EmergencySurgicalHistoryDto
    {
        [JsonPropertyName("surgeryType")]
        public string SurgeryType { get; set; }
        
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
        
        [JsonPropertyName("details")]
        public string Details { get; set; }
    }

    // DTO for completing an emergency request
    public class CompleteEmergencyRequestDto
    {
        [Required]
        public int RequestId { get; set; }
    }

    // DTO for quick emergency request (simplified testing)
    public class QuickEmergencyRequestDto
    {
        public string InjuryDescription { get; set; } // Can be null
        public int? Priority { get; set; } // Nullable - defaults to 1 if null
        
        // Medical history access (both can be null)
        public string MedicalHistoryQrCode { get; set; } // Can be null
        public string MedicalHistoryToken { get; set; } // Can be null
    }
} 