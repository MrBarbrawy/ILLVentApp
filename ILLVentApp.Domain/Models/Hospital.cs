

namespace ILLVentApp.Domain.Models
{
	public class Hospital
	{
		public int HospitalId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public string Phone { get; set; }
		public bool HasContract { get; set; } // Partner status

		// Navigation properties
		public List<Ambulance> Ambulances { get; set; }
		public List<Doctor> Doctors { get; set; }
	}
}
