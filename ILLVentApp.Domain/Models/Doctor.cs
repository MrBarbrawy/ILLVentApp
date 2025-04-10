

namespace ILLVentApp.Domain.Models
{	
	public class Doctor
	{
		public int DoctorId { get; set; }
		public string DoctorName { get; set; }
		public string Specialization { get; set; } // E.g., "Cardiologist"
		public string Phone { get; set; }
		public string Email { get; set; }

		// Foreign key
		public int HospitalId { get; set; }

		// Navigation property
		public Hospital Hospital { get; set; }
	}
}
