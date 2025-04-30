using ILLVentApp.Domain.Models;
using ILLVentApp.Infrastructure.Data.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ILLVentApp.Infrastructure.Data.Seeding
{
    public static class DoctorDataSeeder
    {
        public static async Task SeedDoctorData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            logger.LogInformation("Starting doctor data seeding...");

            // Check if doctors already exist
            if (await context.Doctors.AnyAsync())
            {
                logger.LogInformation("Doctors already exist in the database. Skipping seeding.");
                return;
            }

            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData", "doctors.json");
            
            // Ensure doctors.json exists
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Doctor seed data file not found at {jsonPath}. Please ensure the file exists with valid doctor data.");
            }

            var jsonData = await File.ReadAllTextAsync(jsonPath);
            var doctorData = JsonSerializer.Deserialize<List<DoctorData>>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (doctorData != null)
            {
                var doctors = new List<Doctor>();
                foreach (var data in doctorData)
                {
                    try
                    {
                        var doctor = new Doctor
                        {
                            Name = data.Name,
                            Specialty = data.Specialty,
                            Education = data.Education,
                            Hospital = data.Hospital,
                            Location = data.Location,
                            ImageUrl = data.ImageUrl,
                            Thumbnail = data.Thumbnail,
                            Rating = data.Rating,
                            AcceptInsurance = data.AcceptInsurance,
                            // Set default working hours
                            StartTime = new TimeSpan(9, 0, 0), // 9 AM
                            EndTime = new TimeSpan(17, 0, 0), // 5 PM
                            SlotDurationMinutes = 30, // 30-minute slots
                            WorkingDays = "1,2,3,4,5" // Monday to Friday (1=Monday, 2=Tuesday, etc.)
                        };

                        // Validate the doctor data before adding to the context
                        if (string.IsNullOrWhiteSpace(doctor.Name) || string.IsNullOrWhiteSpace(doctor.Specialty))
                        {
                            logger.LogWarning($"Doctor data is incomplete for: {data.Name}. Skipping this entry.");
                            continue;
                        }

                        // Ensure image paths are valid
                        if (!string.IsNullOrWhiteSpace(doctor.ImageUrl) && !File.Exists(Path.Combine(environment.WebRootPath, doctor.ImageUrl.TrimStart('/'))))
                        {
                            logger.LogWarning($"Image file not found for doctor: {data.Name}. Using default image.");
                            doctor.ImageUrl = "/images/doctors/default.png";
                            doctor.Thumbnail = "/images/doctors/default_thumb.png";
                        }

                        doctors.Add(doctor);
                        logger.LogInformation($"Added doctor: {doctor.Name}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error adding doctor: {data.Name}");
                    }
                }

                await context.Doctors.AddRangeAsync(doctors);
                await context.SaveChangesAsync();

                logger.LogInformation("Doctor data seeding completed successfully.");
            }
        }
    }

    public class DoctorData
    {
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string Education { get; set; }
        public string Hospital { get; set; }
        public string Location { get; set; }
        public string ImageUrl { get; set; }
        public string Thumbnail { get; set; }
        public double Rating { get; set; }
        public bool AcceptInsurance { get; set; }
    }
} 