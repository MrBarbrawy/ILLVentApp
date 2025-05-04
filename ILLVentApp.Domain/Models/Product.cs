using System;
using System.Collections.Generic;

namespace ILLVentApp.Domain.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string Thumbnail { get; set; }
        public double Rating { get; set; }
        public string ProductType { get; set; } // e.g., "CreditCard", "SmartBand", etc.
        public bool HasNFC { get; set; }
        public bool HasMedicalDataStorage { get; set; }
        public bool HasRescueProtocol { get; set; }
        public bool HasVitalSensors { get; set; }
        public string TechnicalDetails { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<OrderItem> OrderItems { get; set; }
    }
} 