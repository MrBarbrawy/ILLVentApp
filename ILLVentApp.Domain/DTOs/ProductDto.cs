using System;

namespace ILLVentApp.Domain.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string Thumbnail { get; set; }
        public double Rating { get; set; }
        public string ProductType { get; set; }
        public bool HasNFC { get; set; }
        public bool HasMedicalDataStorage { get; set; }
        public bool HasRescueProtocol { get; set; }
        public bool HasVitalSensors { get; set; }
        public string TechnicalDetails { get; set; }
        public int StockQuantity { get; set; }
    }
} 