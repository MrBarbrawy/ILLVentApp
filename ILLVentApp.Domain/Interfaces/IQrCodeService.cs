using System;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IQrCodeService
    {
        /// <summary>
        /// Generates a QR code for the given data
        /// </summary>
        /// <param name="data">The data to encode in the QR code</param>
        /// <returns>The generated QR code as a base64 string</returns>
        Task<string> GenerateQrCodeAsync(string data);

        /// <summary>
        /// Validates if a QR code is still valid (not expired and properly encrypted)
        /// </summary>
        /// <param name="qrCode">The QR code to validate</param>
        /// <param name="generatedAt">When the QR code was generated</param>
        /// <param name="expiresAt">When the QR code expires</param>
        /// <returns>True if the QR code is valid, false otherwise</returns>
        Task<bool> ValidateQrCodeAsync(string qrCode, DateTime generatedAt, DateTime expiresAt);

        /// <summary>
        /// Decodes the data from a QR code
        /// </summary>
        /// <param name="qrCode">The QR code to decode</param>
        /// <returns>The decoded data</returns>
        Task<string> DecodeQrCodeAsync(string qrCode);
    }
} 