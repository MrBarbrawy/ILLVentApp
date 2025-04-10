

namespace ILLVentApp.Domain.Models
{
	public class Deals
	{
		public int DealId { get; set; }
		public string DealType { get; set; } // "Hospital", "Pharmacy"
		public string DealDetails { get; set; }
		public DateTime ExpirationDate { get; set; }

		// Foreign key (optional, if linked to a pharmacy)
		public int? PharmacyId { get; set; }

		// Navigation property
		public Pharmacy Pharmacy { get; set; }
	}
}
