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
    public static class HospitalDataSeeder
    {
        public static async Task SeedHospitalData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            logger.LogInformation("Starting hospital data seeding...");

            // Check if hospitals already exist
            if (await context.Hospitals.AnyAsync())
            {
                logger.LogInformation("Hospitals already exist in the database. Skipping seeding.");
                return;
            }

            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData", "hospitals.json");
            
            // Ensure hospitals.json exists
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Hospital seed data file not found at {jsonPath}. Please ensure the file exists with valid hospital data.");
            }

            var jsonData = await File.ReadAllTextAsync(jsonPath);
            var hospitalData = JsonSerializer.Deserialize<List<HospitalData>>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (hospitalData != null)
            {
                foreach (var data in hospitalData)
                {
                    try
                    {
                        var hospital = new Hospital
                        {
                            Name = data.Name,
                            Description = data.Description,
                            Thumbnail = data.Thumbnail,
                            ImageUrl = data.ImageUrl,
							Location = data.Location,
                            ContactNumber = data.ContactNumber,
							Rating = data.Rating,
							Established = data.Established,
							Specialties = data.Specialties,
							IsAvailable = data.IsAvailable, // Default value
                            Latitude = data.Latitude,
                            Longitude = data.Longitude,
                            HasContract = data.HasContract,
						};
						// Validate the hospital data before adding to the context
						if (string.IsNullOrWhiteSpace(hospital.Name) || string.IsNullOrWhiteSpace(hospital.ContactNumber))
						{
							logger.LogWarning($"Hospital data is incomplete for: {data.Name}. Skipping this entry.");
							continue;

						}
						;

                        context.Hospitals.Add(hospital);
                        logger.LogInformation($"Added hospital: {hospital.Name}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error adding hospital: {data.Name}");
                    }
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Hospital data seeding completed successfully.");
            }
        }


    }

    public class HospitalData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool HasContract { get; set; }
        public string Thumbnail { get; set; }
        public string ImageUrl { get; set; }
		public string Location { get; set; }
		public double Rating { get; set; }
		public string ContactNumber { get; set; }

		public List<string> Specialties { get; set; } = new();
		public string Established { get; set; }
		public bool IsAvailable { get; set; } // Default value

	}
}