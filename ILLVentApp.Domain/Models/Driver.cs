

namespace ILLVentApp.Domain.Models
{
	public class Driver
	{
		public int DriverId { get; set; }
		public string DriverName { get; set; }
		public string DriverLicense { get; set; } // Unique
		public string Phone { get; set; }
		public string Email { get; set; }

		// Navigation property
		public List<Ambulance> Ambulances { get; set; }
	}
}
