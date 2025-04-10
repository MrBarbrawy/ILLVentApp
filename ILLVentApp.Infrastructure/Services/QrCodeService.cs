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

        private class QrCodePayload
        {
            public string Id { get; set; }
            public string Data { get; set; }
            public long Timestamp { get; set; }
        }
    }
} 