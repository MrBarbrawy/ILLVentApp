using System;
using System.IO;
using System.Threading.Tasks;
using ILLVentApp.Domain.Interfaces;
using QRCoder;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZXing;

namespace ILLVentApp.Infrastructure.Services
{
    public class QrCodeService : IQrCodeService
    {
        private readonly ILogger<QrCodeService> _logger;
        private readonly string _encryptionKey;

        public QrCodeService(ILogger<QrCodeService> logger, string encryptionKey)
        {
            _logger = logger;
            _encryptionKey = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));
        }

        public async Task<string> GenerateQrCodeAsync(string data)
        {
            try
            {
                // Create a unique identifier for this QR code
                var qrCodeId = Guid.NewGuid().ToString();
                var timestamp = DateTime.UtcNow.Ticks;

                // Create a payload object
                var payload = new
                {
                    Id = qrCodeId,
                    Data = data,
                    Timestamp = timestamp
                };

                // Serialize and encrypt the payload
                var jsonPayload = JsonSerializer.Serialize(payload);
                var encryptedData = EncryptString(jsonPayload);

                // Generate QR code
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(encryptedData, QRCodeGenerator.ECCLevel.Q);
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        var qrCodeImage = qrCode.GetGraphic(20);
                        var base64String = Convert.ToBase64String(qrCodeImage);
                        return await Task.FromResult(base64String);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                throw;
            }
        }

        public async Task<string> GenerateDeterministicQrCodeAsync(string data)
        {
            try
            {
                // Create a deterministic identifier based on the data
                var deterministicId = GenerateDeterministicId(data);
                var fixedTimestamp = 638000000000000000L; // Fixed timestamp for deterministic output

                // Create a payload object with deterministic values
                var payload = new
                {
                    Id = deterministicId,
                    Data = data,
                    Timestamp = fixedTimestamp
                };

                // Serialize and encrypt the payload
                var jsonPayload = JsonSerializer.Serialize(payload);
                var encryptedData = EncryptString(jsonPayload);

                // Generate QR code
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(encryptedData, QRCodeGenerator.ECCLevel.Q);
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        var qrCodeImage = qrCode.GetGraphic(20);
                        var base64String = Convert.ToBase64String(qrCodeImage);
                        return await Task.FromResult(base64String);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating deterministic QR code");
                throw;
            }
        }

        public async Task<bool> ValidateQrCodeAsync(string qrCode, DateTime generatedAt, DateTime expiresAt)
        {
            try
            {
                if (string.IsNullOrEmpty(qrCode))
                {
                    return false;
                }

                var now = DateTime.UtcNow;
                if (now < generatedAt || now > expiresAt)
                {
                    return false;
                }

                // Decrypt and validate the QR code data
                var decryptedData = DecryptString(qrCode);
                var payload = JsonSerializer.Deserialize<QrCodePayload>(decryptedData);

                // Validate timestamp (optional: check if the QR code is too old)
                var qrCodeTime = new DateTime(payload.Timestamp);
                var timeDifference = now - qrCodeTime;
                
                // If the QR code is more than 24 hours old, consider it invalid
                return timeDifference.TotalHours <= 24;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code");
                return false;
            }
        }

        public async Task<string> DecodeQrCodeAsync(string qrCode)
        {
            try
            {
                if (string.IsNullOrEmpty(qrCode))
                {
                    return null;
                }

                // Decrypt the QR code data
                var decryptedData = DecryptString(qrCode);
                var payload = JsonSerializer.Deserialize<QrCodePayload>(decryptedData);

                return await Task.FromResult(payload.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding QR code");
                throw;
            }
        }

        public async Task<string> ReadQrCodeImageAsync(string base64Image)
        {
            try
            {
                if (string.IsNullOrEmpty(base64Image))
                {
                    throw new ArgumentException("Base64 image data is required", nameof(base64Image));
                }

                // Remove data URL prefix if present
                if (base64Image.StartsWith("data:image"))
                {
                    var commaIndex = base64Image.IndexOf(',');
                    if (commaIndex > 0)
                    {
                        base64Image = base64Image.Substring(commaIndex + 1);
                    }
                }

                // Convert base64 to byte array
                var imageBytes = Convert.FromBase64String(base64Image);

                // Load image using SixLabors.ImageSharp
                using (var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(imageBytes))
                {
                    // Convert ImageSharp image to byte array for ZXing
                    using (var memoryStream = new MemoryStream())
                    {
                        image.SaveAsPng(memoryStream);
                        var pngBytes = memoryStream.ToArray();
                        
                        // Create a luminance source from the image
                        var luminanceSource = new ZXing.RGBLuminanceSource(
                            GetRgbValues(image), 
                            image.Width, 
                            image.Height
                        );
                        
                        // Create a binary bitmap
                        var binaryBitmap = new ZXing.Common.HybridBinarizer(luminanceSource);
                        
                        // Create QR code reader
                        var reader = new ZXing.QrCode.QRCodeReader();
                        
                        // Decode the QR code
                        var result = reader.decode(new ZXing.BinaryBitmap(binaryBitmap));
                        
                        if (result != null)
                        {
                            _logger.LogInformation("Successfully read QR code from image. Content length: {Length}", result.Text?.Length ?? 0);
                            return await Task.FromResult(result.Text);
                        }
                        else
                        {
                            _logger.LogWarning("No QR code found in the provided image");
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading QR code from image: {Message}", ex.Message);
                throw new InvalidOperationException($"Failed to read QR code from image: {ex.Message}", ex);
            }
        }

        private byte[] GetRgbValues(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image)
        {
            var rgbValues = new byte[image.Width * image.Height * 3];
            var index = 0;
            
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    rgbValues[index++] = pixel.R;
                    rgbValues[index++] = pixel.G;
                    rgbValues[index++] = pixel.B;
                }
            }
            
            return rgbValues;
        }

        private string EncryptString(string text)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16]; // Zero IV for simplicity, in production use a random IV

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(text);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private string DecryptString(string cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16]; // Zero IV for simplicity, in production use a random IV

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        private string GenerateDeterministicId(string data)
        {
            // Generate a deterministic ID based on the data using SHA256
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data + _encryptionKey));
                return Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-").Substring(0, 32);
            }
        }

        private class QrCodePayload
        {
            public string Id { get; set; }
            public string Data { get; set; }
            public long Timestamp { get; set; }
        }
    }
} 