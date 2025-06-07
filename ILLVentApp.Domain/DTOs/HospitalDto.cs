using System.Collections.Generic;

namespace ILLVentApp.Domain.DTOs
{
    public class HospitalDto
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
        
        // Additional properties for emergency service
        public bool IsAvailable { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool HasContract { get; set; }
    }

    public class CreateHospitalDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Thumbnail { get; set; }
        public string? ImageUrl { get; set; }
        public required string Location { get; set; }
        public required string ContactNumber { get; set; }
        public string? Established { get; set; }
        public List<string> Specialties { get; set; } = new();
        public bool IsAvailable { get; set; } = true;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool HasContract { get; set; } = false;
        public double Rating { get; set; } = 0.0;
    }
}