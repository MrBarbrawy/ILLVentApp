using System.Collections.Generic;

namespace ILLVentApp.Domain.DTOs
{
    public class HospitalDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Thumbnail { get; set; }
        public string? ImageUrl { get; set; }
        public required string Location { get; set; }
        public double Rating { get; set; }
        public required string ContactNumber { get; set; }
        public string? Established { get; set; }
        public List<string> Specialties { get; set; } = new();
    }
}