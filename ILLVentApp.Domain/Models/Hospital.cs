namespace ILLVentApp.Domain.Models
{
	public class Hospital
	{
		public int HospitalId { get; set; }
		public required string Name { get; set; }
		public string? Description { get; set; }
		public string? Thumbnail { get; set; }
		public string? ImageUrl { get; set; }
		public required string Location { get; set; }
		public double Rating { get; set; }
		public required string ContactNumber { get; set; }
		public string? Established { get; set; }
		public List<string> Specialties { get; set; } = new();
		public bool IsAvailable { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public bool HasContract { get; set; } // Partner status
		public string? WebsiteUrl { get; set; } // Website URL for redirection

		// Navigation properties
		public List<Ambulance> Ambulances { get; set; }

		public Hospital()
		{
			Specialties = new List<string>();
			Ambulances = new List<Ambulance>();
			
		}
	}
}
