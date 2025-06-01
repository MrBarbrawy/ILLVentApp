namespace ILLVentApp.Domain.Models
{
	public class EmergencyRescueRequest
	{
		public int RequestId { get; set; }
		public string UserId { get; set; } // Foreign key - required
		public int? AcceptedHospitalId { get; set; } // Foreign key (nullable)

		// Required fields
		public string RequestDescription { get; set; } // Required
		public string RequestStatus { get; set; } // Required - "Pending", "Accepted", "Rejected", "Completed"
		public string InjuryDescription { get; set; } // Required - will have default value if not provided
		public DateTime Timestamp { get; set; } // Required

		// Location tracking for emergency (required for database but set to defaults)
		public double UserLatitude { get; set; } // Required
		public double UserLongitude { get; set; } // Required
		public DateTime LocationTimestamp { get; set; } // Required

		// Optional fields (can be null or empty)
		public string? RequestImage { get; set; } // Optional - encrypted URL
		public string? InjuryPhotoUrl { get; set; } // Optional - encrypted URL

		// Medical history snapshot (optional - JSON string of emergency medical data)
		public string? MedicalHistorySnapshot { get; set; } // Optional
		public string? MedicalHistorySource { get; set; } // Optional - "QRCode", "Token", "None"

		// Hospital notification and response tracking (optional)
		public string? NotifiedHospitalIds { get; set; } // Optional - JSON array of hospital IDs that were notified
		public string? RejectedByHospitalIds { get; set; } // Optional - JSON array of hospital IDs that rejected this request
		public DateTime? HospitalResponseTime { get; set; } // Optional
		public int RequestPriority { get; set; } = 1; // Required with default - 1=High, 2=Medium, 3=Low

		// Navigation properties
		public User User { get; set; }
		public Hospital? AcceptedHospital { get; set; }
	}
}
