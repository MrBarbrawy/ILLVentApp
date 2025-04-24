using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ILLVentApp.Domain.Models;
using ILLVentApp.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ILLVentApp.Infrastructure.Data.Seeding
{
    public static class PharmacyDataSeeder
    {
        public static async Task SeedPharmacyData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            logger.LogInformation("Starting pharmacy data seeding...");

            // Check if pharmacies already exist
            if (await context.Set<Pharmacy>().AnyAsync())
            {
                logger.LogInformation("Pharmacies already exist in the database. Skipping seeding.");
                return;
            }

            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData", "pharmacies.json");
            
            // Ensure pharmacies.json exists
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Pharmacy seed data file not found at {jsonPath}. Please ensure the file exists with valid pharmacy data.");
            }

            var jsonData = await File.ReadAllTextAsync(jsonPath);
            var pharmacyData = JsonSerializer.Deserialize<List<PharmacyData>>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (pharmacyData != null)
            {
                foreach (var data in pharmacyData)
                {
                    try
                    {
                        var pharmacy = new Pharmacy
                        {
                            Name = data.Name,
                            Description = data.Description,
                            Thumbnail = data.Thumbnail,
                            ImageUrl = data.ImageUrl,
                            Location = data.Location,
                            Rating = data.Rating,
                            ContactNumber = data.ContactNumber,
                            AcceptPrivateInsurance = data.AcceptPrivateInsurance,
                            HasContract = data.HasContract
                        };

                        // Validate the pharmacy data before adding to the context
                        if (string.IsNullOrWhiteSpace(pharmacy.Name) || string.IsNullOrWhiteSpace(pharmacy.ContactNumber))
                        {
                            logger.LogWarning($"Pharmacy data is incomplete for: {data.Name}. Skipping this entry.");
                            continue;
                        }

                        context.Set<Pharmacy>().Add(pharmacy);
                        logger.LogInformation($"Added pharmacy: {pharmacy.Name}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error adding pharmacy: {data.Name}");
                    }
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Pharmacy data seeding completed successfully.");
            }
        }
    }

    public class PharmacyData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Thumbnail { get; set; }
        public string ImageUrl { get; set; }
        public string Location { get; set; }
        public double Rating { get; set; }
        public string ContactNumber { get; set; }
        public bool AcceptPrivateInsurance { get; set; }
        public bool HasContract { get; set; }
    }
} 