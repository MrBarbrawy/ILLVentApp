namespace ILLVentApp.Domain.Models
{
	public class Pharmacy
	{
		public int PharmacyId { get; set; }
		public required string Name { get; set; }
		public string? Description { get; set; }
		public string? Thumbnail { get; set; }
		public string? ImageUrl { get; set; }
		public required string Location { get; set; }
		public double Rating { get; set; }
		public required string ContactNumber { get; set; }
		public bool AcceptPrivateInsurance { get; set; }
		public bool HasContract { get; set; }

		// Navigation property
		public List<Deals> Deals { get; set; }
	}
}
