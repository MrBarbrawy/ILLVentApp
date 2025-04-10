

namespace ILLVentApp.Domain.Models
{
	public class EmergencyRescueRequest
	{
		public int RequestId { get; set; }
		public string UserId { get; set; } // Foreign key
		public int? AcceptedHospitalId { get; set; } // Foreign key (nullable)

		public string RequestDescription { get; set; }
		public string RequestImage { get; set; } // Encrypted URL
		public string RequestStatus { get; set; } // "Pending", "Accepted", "Rejected"
		public string InjuryDescription { get; set; } // Encrypted
		public string InjuryPhotoUrl { get; set; } // Encrypted URL
		public DateTime Timestamp { get; set; }

		// Navigation properties
		public User User { get; set; }
		public Hospital AcceptedHospital { get; set; }
	}
}
