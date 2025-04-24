using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ILLVentApp.Domain.Models;
using ILLVentApp.Infrastructure.Data.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ILLVentApp.Infrastructure.Data.Seeding
{
    public static class PharmacyImageSeeder
    {
        public static async Task SeedPharmacyImages(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            logger.LogInformation("Starting pharmacy image processing...");

            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData", "pharmacy-images.json");
            if (!File.Exists(jsonPath))
            {
                logger.LogWarning("pharmacy-images.json not found. Skipping image processing.");
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var pharmacyImages = JsonSerializer.Deserialize<List<PharmacyImage>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (pharmacyImages != null)
            {
                var wwwrootPath = environment.WebRootPath;
                // Source directory for original images
                var sourceImagesPath = Path.Combine(wwwrootPath, "images", "pharmacies", "source");
                // Output directories for processed images
                var pharmacyImagesPath = Path.Combine(wwwrootPath, "images", "pharmacies");
                var thumbnailsPath = Path.Combine(pharmacyImagesPath, "thumbnails");
                var fullImagesPath = Path.Combine(pharmacyImagesPath, "full");

                // Ensure directories exist
                Directory.CreateDirectory(sourceImagesPath);
                Directory.CreateDirectory(thumbnailsPath);
                Directory.CreateDirectory(fullImagesPath);

                foreach (var pharmacyImage in pharmacyImages)
                {
                    var pharmacy = await context.Set<Pharmacy>()
                        .FirstOrDefaultAsync(h => h.Name == pharmacyImage.Name);

                    if (pharmacy != null)
                    {
                        try
                        {
                            logger.LogInformation($"Processing images for pharmacy: {pharmacy.Name}");

                            // Generate unique filenames based on pharmacy name
                            var safeFileName = MakeFileNameSafe(pharmacy.Name);
                            var thumbnailFileName = $"{safeFileName}_thumb.png";
                            var fullImageFileName = $"{safeFileName}.png";

                            // Get the source image path
                            var sourceImagePath = Path.Combine(wwwrootPath, pharmacyImage.ImageUrl.TrimStart('/'));
                            logger.LogInformation($"Looking for source image at: {sourceImagePath}");

                            if (File.Exists(sourceImagePath))
                            {
                                // Process and save thumbnail
                                var thumbnailPath = Path.Combine(thumbnailsPath, thumbnailFileName);
                                await ProcessImage(sourceImagePath, thumbnailPath, 300, 200);
                                pharmacy.Thumbnail = $"/images/pharmacies/thumbnails/{thumbnailFileName}";

                                // Process and save full image
                                var fullImagePath = Path.Combine(fullImagesPath, fullImageFileName);
                                await ProcessImage(sourceImagePath, fullImagePath, 800, 600);
                                pharmacy.ImageUrl = $"/images/pharmacies/full/{fullImageFileName}";

                                await context.SaveChangesAsync();
                                logger.LogInformation($"Successfully processed images for pharmacy: {pharmacy.Name}");
                            }
                            else
                            {
                                logger.LogWarning($"Source image not found for pharmacy: {pharmacy.Name} at path: {sourceImagePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Error processing images for pharmacy: {pharmacy.Name}");
                        }
                    }
                }
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

        private static async Task ProcessImage(string sourcePath, string outputPath, int width, int height)
        {
            using var image = await Image.LoadAsync(sourcePath);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));
            await image.SaveAsync(outputPath);
        }

        private class PharmacyImage
        {
            public string Name { get; set; }
            public string Thumbnail { get; set; }
            public string ImageUrl { get; set; }
        }
    }
} 