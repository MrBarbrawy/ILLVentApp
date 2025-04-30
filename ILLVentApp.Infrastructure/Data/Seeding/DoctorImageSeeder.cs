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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace ILLVentApp.Infrastructure.Data.Seeding
{
    public static class DoctorImageSeeder
    {
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png" };
        private const string DefaultImageName = "default-doctor.png";
        
        public static async Task SeedDoctorImages(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            logger.LogInformation("Starting doctor image processing...");

            var wwwrootPath = environment.WebRootPath;
            var doctorImagesPath = Path.Combine(wwwrootPath, "images", "doctors");
            var sourceImagesPath = Path.Combine(doctorImagesPath, "source");
            var thumbnailsPath = Path.Combine(doctorImagesPath, "thumbnails");
            var fullImagesPath = Path.Combine(doctorImagesPath, "full");

            // Ensure directories exist
            Directory.CreateDirectory(sourceImagesPath);
            Directory.CreateDirectory(thumbnailsPath);
            Directory.CreateDirectory(fullImagesPath);

            // Copy default image if it doesn't exist
            var defaultSourcePath = Path.Combine(sourceImagesPath, DefaultImageName);
            var defaultFullPath = Path.Combine(fullImagesPath, DefaultImageName);
            var defaultThumbPath = Path.Combine(thumbnailsPath, $"default-doctor_thumb.png");

            if (!File.Exists(defaultSourcePath))
            {
                logger.LogWarning($"Default doctor image not found at {defaultSourcePath}. Creating a placeholder.");
                await CreateDefaultImage(defaultSourcePath);
            }

            // Process default image
            await ProcessImage(defaultSourcePath, defaultFullPath, defaultThumbPath, logger);

            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData", "doctor-images.json");
            if (!File.Exists(jsonPath))
            {
                logger.LogWarning("doctor-images.json not found. Using default images for all doctors.");
                await UpdateDoctorsWithDefaultImages(context, logger);
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var doctorImages = JsonSerializer.Deserialize<List<DoctorImage>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (doctorImages == null)
            {
                logger.LogWarning("No doctor images found in doctor-images.json. Using default images.");
                await UpdateDoctorsWithDefaultImages(context, logger);
                return;
            }

            foreach (var doctorImage in doctorImages)
            {
                var doctor = await context.Doctors
                    .FirstOrDefaultAsync(h => h.Name == doctorImage.Name);

                if (doctor == null)
                {
                    logger.LogWarning($"Doctor not found for image: {doctorImage.Name}");
                    continue;
                }

                try
                {
                    logger.LogInformation($"Processing images for doctor: {doctor.Name}");

                    var safeFileName = MakeFileNameSafe(doctor.Name);
                    var thumbnailFileName = $"{safeFileName}_thumb.png";
                    var fullImageFileName = $"{safeFileName}.png";

                    var sourceImagePath = Path.Combine(wwwrootPath, doctorImage.ImageUrl.TrimStart('/'));
                    var thumbnailPath = Path.Combine(thumbnailsPath, thumbnailFileName);
                    var fullImagePath = Path.Combine(fullImagesPath, fullImageFileName);

                    if (!File.Exists(sourceImagePath))
                    {
                        logger.LogWarning($"Source image not found for doctor: {doctor.Name}. Using default image.");
                        doctor.ImageUrl = $"/images/doctors/full/{DefaultImageName}";
                        doctor.Thumbnail = $"/images/doctors/thumbnails/default-doctor_thumb.png";
                        continue;
                    }

                    // Process images
                    await ProcessImage(sourceImagePath, fullImagePath, thumbnailPath, logger);

                    // Update doctor with processed image paths
                    doctor.ImageUrl = $"/images/doctors/full/{fullImageFileName}";
                    doctor.Thumbnail = $"/images/doctors/thumbnails/{thumbnailFileName}";

                    logger.LogInformation($"Successfully processed images for doctor: {doctor.Name}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing images for doctor: {doctor.Name}. Using default image.");
                    doctor.ImageUrl = $"/images/doctors/full/{DefaultImageName}";
                    doctor.Thumbnail = $"/images/doctors/thumbnails/default-doctor_thumb.png";
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Doctor image processing completed.");
        }

        private static async Task ProcessImage(string sourcePath, string fullPath, string thumbnailPath, ILogger logger)
        {
            try
            {
                // Load the source image
                using var image = await Image.LoadAsync(sourcePath);
                
                // Process and save thumbnail
                using var thumbnail = Image.Load(sourcePath);
                thumbnail.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(200, 200),
                        Mode = ResizeMode.Crop
                    }));
                await thumbnail.SaveAsPngAsync(thumbnailPath, new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression
                });

                // Process and save full image
                image.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(800, 800),
                        Mode = ResizeMode.Max
                    }));
                await image.SaveAsPngAsync(fullPath, new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing image: {sourcePath}");
                throw;
            }
        }

        private static async Task CreateDefaultImage(string path)
        {
            using var image = new Image<Rgba32>(400, 400);
            image.Mutate(x => x.BackgroundColor(Color.LightGray));
            await image.SaveAsPngAsync(path);
        }

        private static async Task UpdateDoctorsWithDefaultImages(AppDbContext context, ILogger logger)
        {
            var doctors = await context.Doctors.ToListAsync();
            foreach (var doctor in doctors)
            {
                doctor.ImageUrl = $"/images/doctors/full/{DefaultImageName}";
                doctor.Thumbnail = $"/images/doctors/thumbnails/default-doctor_thumb.png";
            }
            await context.SaveChangesAsync();
            logger.LogInformation("Updated all doctors with default images.");
        }

        private static string MakeFileNameSafe(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(fileName
                .Where(ch => !invalidChars.Contains(ch))
                .Select(ch => ch == ' ' ? '-' : ch)
                .ToArray())
                .ToLower();
            return safeName;
        }
    }

    public class DoctorImage
    {
        public string Name { get; set; }
        public string Thumbnail { get; set; }
        public string ImageUrl { get; set; }
    }
} 