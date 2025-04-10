

namespace ILLVentApp.Domain.Models
{
	public class Ambulance
	{
		public int AmbulanceId { get; set; }
		public string AmbulancePlateNumber { get; set; } // Unique
		public int Capacity { get; set; }
		public string Equipments { get; set; } // E.g., "Defibrillator, Oxygen"

		// Foreign keys
		public int DriverId { get; set; }
		public int HospitalId { get; set; }

		// Navigation properties
		public Driver Driver { get; set; }
		public Hospital Hospital { get; set; }
	}
}
