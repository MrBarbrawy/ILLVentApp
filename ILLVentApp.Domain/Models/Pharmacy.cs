

namespace ILLVentApp.Domain.Models
{
	public class Pharmacy
	{
		public int PharmacyId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public string Phone { get; set; }
		public bool HasContract { get; set; } // Partner status

		// Navigation property
		public List<Deals> Deals { get; set; }
	}
}
