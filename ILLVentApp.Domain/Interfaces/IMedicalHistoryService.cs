using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Models;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IMedicalHistoryService
    {
        // Save all medical history for a user
        Task<MedicalHistoryResult> SaveMedicalHistoryAsync(SaveMedicalHistoryCommand command, string userId);
        
        // Update medical history for a user
        Task<MedicalHistoryResult> UpdateMedicalHistoryAsync(SaveMedicalHistoryCommand command, string userId);
        
        // Get medical history by user ID
        Task<MedicalHistoryResult> GetMedicalHistoryByUserIdAsync(string userId);
        
        // Generate QR code data for a user ID
        Task<QrCodeResult> GenerateQrCodeAsync(string userId);
        
        // Get medical history using QR code data
        Task<MedicalHistoryResult> GetMedicalHistoryByQrCodeAsync(string qrCodeData);
    }
} 