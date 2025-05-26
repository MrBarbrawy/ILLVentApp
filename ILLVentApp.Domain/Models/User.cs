using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ILLVentApp.Domain.Models
{

	public class User : IdentityUser
	{
		// Required Fields (■)
		[Required]
		[MaxLength(50)]
		public string FirstName { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string Surname { get; set; } = string.Empty;

		[Required]
		[MaxLength(255)]
		public string QrCode { get; set; } = Guid.NewGuid().ToString();

		[Required]
		[MaxLength(500)]
		public string Address { get; set; } = "Pending";

		[Required]
		public uint SecurityVersion { get; set; } = 1;

		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[Required]
		public bool IsEmailVerified { get; set; } = false;

		// Nullable Fields (☑)
		public string? Otp { get; set; }
		public DateTime? OtpExpiry { get; set; }

		public MedicalHistory MedicalHistory { get; set; }
		public List<EmergencyRescueRequest> EmergencyRescueRequests { get; set; }
		}




	
}
