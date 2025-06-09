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

    // COMPREHENSIVE EMERGENCY ERROR HANDLING RESULT CLASSES
    
    // Main result class for emergency operations
    public class EmergencyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public EmergencyRequestResponseDto Data { get; set; }
        public List<string> Errors { get; set; }
        public string ErrorCategory { get; set; }
        public int? ErrorCode { get; set; }

        public static EmergencyResult Successful(EmergencyRequestResponseDto data = null, string message = "Emergency request processed successfully")
        {
            return new EmergencyResult
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = new List<string>(),
                ErrorCategory = null,
                ErrorCode = null
            };
        }

        public static EmergencyResult Failure(string message, List<string> errors = null, string category = "General", int? errorCode = null)
        {
            return new EmergencyResult
            {
                Success = false,
                Message = message,
                Data = null,
                Errors = errors ?? new List<string>(),
                ErrorCategory = category,
                ErrorCode = errorCode
            };
        }

        // 🔥 CRITICAL SYSTEM ERRORS - Database, network, core failures
        public static EmergencyResult DatabaseConnectionLost()
        {
            return Failure(
                "Emergency system temporarily unavailable. Please call 911 directly", 
                new List<string> { "Database connection lost", "Emergency services should be contacted directly" },
                "CriticalSystem", 5001
            );
        }

        public static EmergencyResult SignalRConnectionFailed()
        {
            return Failure(
                "Real-time notifications unavailable. Your request is logged but hospitals may not be notified immediately",
                new List<string> { "SignalR hub connection failed", "Hospital notifications may be delayed" },
                "CriticalSystem", 5002
            );
        }

        public static EmergencyResult CoreSystemFailure()
        {
            return Failure(
                "Critical system error detected. Please call emergency services directly at 911",
                new List<string> { "Core emergency system failure", "Manual emergency services required" },
                "CriticalSystem", 5000
            );
        }

        public static EmergencyResult NetworkTimeout()
        {
            return Failure(
                "Network connectivity issues detected. Request saved locally and will retry automatically",
                new List<string> { "Network timeout", "Request queued for retry" },
                "CriticalSystem", 5003
            );
        }

        // 🏥 HOSPITAL AVAILABILITY - No hospitals, all rejected scenarios
        public static EmergencyResult NoHospitalsConfigured()
        {
            return Failure(
                "No emergency hospitals configured in the system. Please contact system administrator",
                new List<string> { "No target hospitals found in configuration", "System configuration error" },
                "HospitalAvailability", 4001
            );
        }

        public static EmergencyResult AllHospitalsUnavailable()
        {
            return Failure(
                "All configured hospitals are currently unavailable. Please call 123 directly for immediate assistance",
                new List<string> { "No available hospitals found", "All hospitals marked as unavailable" },
                "HospitalAvailability", 4002
            );
        }

        public static EmergencyResult AllHospitalsRejected()
        {
            return Failure(
                "Unfortunately, no hospitals can currently accept this emergency request. Emergency backup services are being contacted",
                new List<string> { "All hospitals rejected the request", "Backup emergency services initiated" },
                "HospitalAvailability", 4003
            );
        }

        public static EmergencyResult HospitalSystemDown()
        {
            return Failure(
                "Hospital notification system is temporarily down. Manual emergency dispatch has been initiated",
                new List<string> { "Hospital communication system unavailable", "Manual dispatch procedures activated" },
                "HospitalAvailability", 4004
            );
        }

        public static EmergencyResult NoHospitalsInRange()
        {
            return Failure(
                "No emergency hospitals found within reasonable distance. Expanding search radius and contacting regional services",
                new List<string> { "No hospitals within standard range", "Regional emergency services contacted" },
                "HospitalAvailability", 4005
            );
        }

        // 🔐 AUTHENTICATION/SECURITY - Access control and abuse prevention  
        public static EmergencyResult UserNotAuthenticated()
        {
            return Failure(
                "Authentication required to submit emergency requests. Please login to continue",
                new List<string> { "User not authenticated", "Emergency access requires valid user session" },
                "Authentication", 3001
            );
        }

        public static EmergencyResult EmergencyAccessDenied()
        {
            return Failure(
                "Emergency system access denied. Please contact system administrator if this is urgent",
                new List<string> { "Emergency access restricted for this user", "Administrator intervention required" },
                "Authentication", 3002
            );
        }

        public static EmergencyResult TooManyRequests()
        {
            return Failure(
                "Too many emergency requests submitted recently. Please wait 5 minutes before submitting another request",
                new List<string> { "Rate limit exceeded", "Multiple emergency requests detected" },
                "Authentication", 3003
            );
        }

        public static EmergencyResult SuspiciousActivity()
        {
            return Failure(
                "Suspicious activity detected. This request has been flagged for manual review by emergency personnel",
                new List<string> { "Security alert triggered", "Manual review required" },
                "Authentication", 3004
            );
        }

        public static EmergencyResult AccountLocked()
        {
            return Failure(
                "User account is temporarily locked. Please contact support or call 911 directly for emergencies",
                new List<string> { "Account security lock active", "Direct emergency services recommended" },
                "Authentication", 3005
            );
        }

        // 📝 MEDICAL HISTORY - QR code and token validation
        public static EmergencyResult InvalidQRCode()
        {
            return Failure(
                "Invalid or expired QR code provided. Emergency request will proceed without medical history",
                new List<string> { "QR code validation failed", "Proceeding without medical history" },
                "MedicalHistory", 2001
            );
        }

        public static EmergencyResult InvalidMedicalToken()
        {
            return Failure(
                "Invalid medical history token provided. Emergency request will proceed without medical history",
                new List<string> { "Medical token validation failed", "Proceeding without medical history" },
                "MedicalHistory", 2002
            );
        }

        public static EmergencyResult MedicalHistoryServiceDown()
        {
            return Failure(
                "Medical history service is temporarily unavailable. Emergency request will proceed without medical history",
                new List<string> { "Medical history service unreachable", "Emergency processing continues" },
                "MedicalHistory", 2003
            );
        }

        public static EmergencyResult CorruptedMedicalData()
        {
            return Failure(
                "Medical history data appears corrupted. Emergency request will proceed with basic information only",
                new List<string> { "Medical data integrity check failed", "Using fallback emergency processing" },
                "MedicalHistory", 2004
            );
        }

        // 📡 COMMUNICATION - SignalR and notification systems
        public static EmergencyResult NotificationDeliveryFailed()
        {
            return Failure(
                "Unable to notify hospitals in real-time. Emergency request logged and backup notification methods activated",
                new List<string> { "Real-time notification failed", "Backup notification systems activated" },
                "Communication", 1001
            );
        }

        public static EmergencyResult HospitalCommunicationDown()
        {
            return Failure(
                "Communication with hospital systems is disrupted. Emergency services are being contacted directly",
                new List<string> { "Hospital communication system down", "Direct emergency services contacted" },
                "Communication", 1002
            );
        }

        public static EmergencyResult SignalRHubUnavailable()
        {
            return Failure(
                "Real-time tracking system is unavailable. Your emergency request is still active but live updates may be limited",
                new List<string> { "SignalR hub connection lost", "Live tracking temporarily unavailable" },
                "Communication", 1003
            );
        }

        public static EmergencyResult NotificationQueueFull()
        {
            return Failure(
                "Emergency notification system is at capacity. Your request has priority queuing and will be processed immediately",
                new List<string> { "Notification queue at capacity", "Priority emergency processing activated" },
                "Communication", 1004
            );
        }

        // 📸 MEDIA UPLOAD - Photo handling and storage
        public static EmergencyResult PhotoUploadFailed()
        {
            return Failure(
                "Unable to upload injury photo at this time. Emergency request will proceed with description only",
                new List<string> { "Photo upload service unavailable", "Processing with text description" },
                "MediaUpload", 6001
            );
        }

        public static EmergencyResult PhotoTooLarge()
        {
            return Failure(
                "Injury photo file is too large. Please use a smaller image or proceed without photo",
                new List<string> { "Image file size exceeds limit", "Compression or alternative image needed" },
                "MediaUpload", 6002
            );
        }

        public static EmergencyResult InvalidPhotoFormat()
        {
            return Failure(
                "Invalid photo format provided. Please use JPG, PNG, or GIF format, or proceed without photo",
                new List<string> { "Unsupported image format", "Valid formats: JPG, PNG, GIF" },
                "MediaUpload", 6003
            );
        }

        public static EmergencyResult StorageServiceDown()
        {
            return Failure(
                "Photo storage service is temporarily unavailable. Emergency request will proceed without image storage",
                new List<string> { "Cloud storage service down", "Emergency processing continues" },
                "MediaUpload", 6004
            );
        }

        // Additional helper methods for existing request handling
        public static EmergencyResult ExistingActiveRequest(int existingRequestId)
        {
            return Failure(
                "You already have an active emergency request. Please wait for current request to be resolved or contact emergency services directly",
                new List<string> { $"Active request ID: {existingRequestId}", "Only one emergency request allowed at a time" },
                "RequestValidation", 7001
            );
        }

        public static EmergencyResult InvalidPriority()
        {
            return Failure(
                "Invalid priority level specified. Using default high priority for emergency request",
                new List<string> { "Priority level out of range", "Defaulting to high priority" },
                "RequestValidation", 7002
            );
        }

        public static EmergencyResult LocationValidationFailed()
        {
            return Failure(
                "Unable to validate your location. Using default location and notifying nearby hospitals",
                new List<string> { "GPS coordinates validation failed", "Using fallback location services" },
                "RequestValidation", 7003
            );
        }
    }

    // Hospital response result class
    public class HospitalResponseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ResponseType { get; set; } // "Accepted", "Rejected", "Error"
        public int? EstimatedResponseTime { get; set; }

        public static HospitalResponseResult Accepted(int estimatedMinutes)
        {
            return new HospitalResponseResult
            {
                Success = true,
                Message = $"Emergency request accepted. Estimated response time: {estimatedMinutes} minutes",
                ResponseType = "Accepted",
                EstimatedResponseTime = estimatedMinutes
            };
        }

        public static HospitalResponseResult Rejected(string reason)
        {
            return new HospitalResponseResult
            {
                Success = false,
                Message = $"Emergency request rejected: {reason}",
                ResponseType = "Rejected"
            };
        }

        public static HospitalResponseResult Error(string error)
        {
            return new HospitalResponseResult
            {
                Success = false,
                Message = $"Error processing hospital response: {error}",
                ResponseType = "Error"
            };
        }
    }

    // Location update result class
    public class LocationUpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public double? UpdatedLatitude { get; set; }
        public double? UpdatedLongitude { get; set; }
        public DateTime? UpdatedTimestamp { get; set; }

        public static LocationUpdateResult Successful(double latitude, double longitude)
        {
            return new LocationUpdateResult
            {
                Success = true,
                Message = "Location updated successfully",
                UpdatedLatitude = latitude,
                UpdatedLongitude = longitude,
                UpdatedTimestamp = DateTime.UtcNow
            };
        }

        public static LocationUpdateResult Failed(string reason)
        {
            return new LocationUpdateResult
            {
                Success = false,
                Message = $"Location update failed: {reason}"
            };
        }
    }
} 