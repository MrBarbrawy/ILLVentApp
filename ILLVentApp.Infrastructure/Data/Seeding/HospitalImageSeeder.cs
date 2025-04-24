using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ILLVentApp.Domain.Models;
using ILLVentApp.Infrastructure.Data.Contexts;
using ILLVentApp.Infrastructure.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ILLVentApp.Infrastructure.Data.Seeding
{
    public static class HospitalImageSeeder
    {
        public static async Task SeedHospitalImages(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData", "hospital-images.json");
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Hospital images seed data file not found at {jsonPath}");
            }

            var jsonData = await File.ReadAllTextAsync(jsonPath);
            var hospitalImages = JsonSerializer.Deserialize<List<HospitalImageData>>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (hospitalImages != null)
            {
                var wwwrootPath = environment.WebRootPath;
                // Source directory for original images
                var sourceImagesPath = Path.Combine(wwwrootPath, "images", "hospitals", "source");
                // Output directories for processed images
                var hospitalImagesPath = Path.Combine(wwwrootPath, "images", "hospitals");
                var thumbnailsPath = Path.Combine(hospitalImagesPath, "thumbnails");
                var fullImagesPath = Path.Combine(hospitalImagesPath, "full");

                // Ensure directories exist
                Directory.CreateDirectory(sourceImagesPath);
                Directory.CreateDirectory(thumbnailsPath);
                Directory.CreateDirectory(fullImagesPath);

                foreach (var hospitalImage in hospitalImages)
                {
                    var hospital = await context.Set<Hospital>()
                        .FirstOrDefaultAsync(h => h.Name == hospitalImage.Name);

                    if (hospital != null)
                    {
                        try
                        {
                            logger.LogInformation($"Processing images for hospital: {hospital.Name}");

                            // Generate unique filenames based on hospital name
                            var safeFileName = MakeFileNameSafe(hospital.Name);
                            var thumbnailFileName = $"{safeFileName}_thumb.png";
                            var fullImageFileName = $"{safeFileName}.png";

                            // Get the source image path
                            var sourceImagePath = Path.Combine(wwwrootPath, hospitalImage.ImageUrl.TrimStart('/'));
                            logger.LogInformation($"Looking for source image at: {sourceImagePath}");

                            if (!File.Exists(sourceImagePath))
                            {
                                throw new FileNotFoundException($"Source image not found at {sourceImagePath}");
                            }

                            // Process and save thumbnail
                            var thumbnailPath = Path.Combine(thumbnailsPath, thumbnailFileName);
                            await ImageProcessor.SaveImageFromUrlOrPath(sourceImagePath, thumbnailPath, maxWidth: 200);
                            logger.LogInformation($"Saved thumbnail to: {thumbnailPath}");

                            // Process and save full image
                            var fullImagePath = Path.Combine(fullImagesPath, fullImageFileName);
                            await ImageProcessor.SaveImageFromUrlOrPath(sourceImagePath, fullImagePath, maxWidth: 800);
                            logger.LogInformation($"Saved full image to: {fullImagePath}");

                            // Update database with new paths
                            hospital.Thumbnail = $"/images/hospitals/thumbnails/{thumbnailFileName}";
                            hospital.ImageUrl = $"/images/hospitals/full/{fullImageFileName}";

                            logger.LogInformation($"Successfully processed images for hospital: {hospital.Name}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Error processing images for hospital: {hospital.Name}");
                        }
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        private static string MakeFileNameSafe(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
                .Replace(" ", "_")
                .ToLowerInvariant();
            return safeName;
        }
    }

    public class HospitalImageData
    {
        public string Name { get; set; }
        public string Thumbnail { get; set; }
        public string ImageUrl { get; set; }
    }
} 