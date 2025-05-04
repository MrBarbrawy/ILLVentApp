using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ILLVentApp.Domain.Models;
using ILLVentApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ILLVentApp.Infrastructure.Data.Seeding
{
    public class ProductSeeder
    {
        public static async Task SeedProductsAsync(IAppDbContext context)
        {
            // Check if products already exist
            if (await context.Products.AnyAsync())
                return;

            var products = new List<Product>
            {
                // Credit Card (Normal)
                new Product
                {
                    Name = "Credit Card (Normal)",
                    Description = "A smart Card with NFC Technology Specially Customized Per User loaded with his Medical History and Engraved User ID and QR Code.",
                    Price = 150M,
                    ImageUrl = "/images/products/source/credit-card-normal.jpg",
                    Thumbnail = "/images/products/source/credit-card-normal.jpg",
                    Rating = 4.0,
                    ProductType = "CreditCard",
                    HasNFC = true,
                    HasMedicalDataStorage = true,
                    HasRescueProtocol = true,
                    HasVitalSensors = false,
                    TechnicalDetails = "NFC technology; Medical data storage; QR code; User ID engraving",
                    StockQuantity = 100,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Credit Card (Premium)
                new Product
                {
                    Name = "Credit Card (Premium)",
                    Description = "A smart Card with NFC Technology Specially Customized Per User loaded with his Medical History and Engraved User ID and QR Code in addition to Payment gateway.",
                    Price = 350M,
                    ImageUrl = "/images/products/source/credit-card-premium.jpg",
                    Thumbnail = "/images/products/source/credit-card-premium.jpg",
                    Rating = 4.0,
                    ProductType = "CreditCard",
                    HasNFC = true,
                    HasMedicalDataStorage = true,
                    HasRescueProtocol = true,
                    HasVitalSensors = false,
                    TechnicalDetails = "NFC technology; Medical data storage; QR code; User ID engraving; Payment gateway integration",
                    StockQuantity = 50,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Metal KeyChain
                new Product
                {
                    Name = "Metal KeyChain",
                    Description = "A Metal Key Chain With the User ID and QR Code To Facilitate the Rescue Protocol and carriers NFC Technology.",
                    Price = 75M,
                    ImageUrl = "/images/products/source/metal-keychain.jpg",
                    Thumbnail = "/images/products/source/metal-keychain.jpg",
                    Rating = 4.0,
                    ProductType = "KeyChain",
                    HasNFC = true,
                    HasMedicalDataStorage = true,
                    HasRescueProtocol = true,
                    HasVitalSensors = false,
                    TechnicalDetails = "Metal construction; Engraved QR code; NFC chip; Durable",
                    StockQuantity = 200,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Aluminum Necklace
                new Product
                {
                    Name = "Aluminum Necklace",
                    Description = "An Aluminum Necklace With the User ID and QR Code To Facilitate the Rescue Protocol.",
                    Price = 200M,
                    ImageUrl = "/images/products/source/aluminum-necklace.jpg",
                    Thumbnail = "/images/products/source/aluminum-necklace.jpg",
                    Rating = 4.0,
                    ProductType = "Necklace",
                    HasNFC = true,
                    HasMedicalDataStorage = true,
                    HasRescueProtocol = true,
                    HasVitalSensors = false,
                    TechnicalDetails = "Aluminum construction; Adjustable chain; Waterproof; Engraved QR code",
                    StockQuantity = 150,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Smart Band
                new Product
                {
                    Name = "Smart Band",
                    Description = "A Smart Band with Vital Sensors Technology Specially Designed to watch Vital Readings and call for Rescue when Needed.",
                    Price = 2600M,
                    ImageUrl = "/images/products/source/smart-band.jpg",
                    Thumbnail = "/images/products/source/smart-band.jpg",
                    Rating = 4.0,
                    ProductType = "SmartBand",
                    HasNFC = true,
                    HasMedicalDataStorage = true,
                    HasRescueProtocol = true,
                    HasVitalSensors = true,
                    TechnicalDetails = "Heart rate monitor; Blood oxygen sensor; Temperature sensor; Emergency alert system; Bluetooth connectivity; Rechargeable battery",
                    StockQuantity = 30,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Smart Watch
                new Product
                {
                    Name = "Smart Watch",
                    Description = "A Smart Watch with Vital Sensors Technology Specially Designed to watch Vital Readings and call for Rescue when Needed and Fancy look and Interface.",
                    Price = 4300M,
                    ImageUrl = "/images/products/source/smart-watch.jpg",
                    Thumbnail = "/images/products/source/smart-watch.jpg",
                    Rating = 4.0,
                    ProductType = "SmartWatch",
                    HasNFC = true,
                    HasMedicalDataStorage = true,
                    HasRescueProtocol = true,
                    HasVitalSensors = true,
                    TechnicalDetails = "Heart rate monitor; Blood oxygen sensor; Temperature sensor; ECG; Blood pressure monitor; Emergency alert system; Bluetooth connectivity; Rechargeable battery; Customizable watch faces; Water resistant",
                    StockQuantity = 20,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            // Add products to database
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
} 