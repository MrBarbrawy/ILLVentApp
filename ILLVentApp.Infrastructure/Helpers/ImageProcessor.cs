using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ILLVentApp.Infrastructure.Helpers
{
    public static class ImageProcessor
    {
        public static async Task SaveImageFromUrlOrPath(string sourceUrl, string destinationPath, int? maxWidth = null)
        {
            try
            {
                // Create the directory if it doesn't exist
                var directory = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // If the source is a URL, download it
                if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uriResult) 
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    using var client = new HttpClient();
                    var imageBytes = await client.GetByteArrayAsync(sourceUrl);
                    
                    using var image = Image.Load(imageBytes);
                    if (maxWidth.HasValue && image.Width > maxWidth.Value)
                    {
                        var scaleFactor = (float)maxWidth.Value / image.Width;
                        var newHeight = (int)(image.Height * scaleFactor);
                        
                        image.Mutate(x => x.Resize(maxWidth.Value, newHeight));
                    }
                    
                    await image.SaveAsPngAsync(destinationPath);
                }
                // If it's a local file, process and save it
                else if (File.Exists(sourceUrl))
                {
                    using var image = Image.Load(sourceUrl);
                    if (maxWidth.HasValue && image.Width > maxWidth.Value)
                    {
                        var scaleFactor = (float)maxWidth.Value / image.Width;
                        var newHeight = (int)(image.Height * scaleFactor);
                        
                        image.Mutate(x => x.Resize(maxWidth.Value, newHeight));
                    }
                    
                    await image.SaveAsPngAsync(destinationPath);
                }
                else
                {
                    throw new FileNotFoundException($"Source image not found: {sourceUrl}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing image from {sourceUrl}: {ex.Message}", ex);
            }
        }
    }
} 