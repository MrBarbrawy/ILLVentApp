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
        
        // SECURE ACCESS: Get medical history using QR code data - requires ownership validation
        Task<MedicalHistoryResult> GetMedicalHistoryByQrCodeAsync(string qrCodeData, string userId);

        // SECURE ACCESS: Get medical history using token directly - requires ownership validation
        Task<MedicalHistoryResult> GetMedicalHistoryByTokenAsync(string token);
        
        // EMERGENCY ACCESS: Get medical history using QR code data - no ownership validation
        Task<MedicalHistoryResult> GetEmergencyMedicalHistoryByQrCodeAsync(string qrCodeData, string emergencyReason);
        
        // EMERGENCY ACCESS: Get medical history using token directly - no ownership validation
        Task<MedicalHistoryResult> GetEmergencyMedicalHistoryByTokenAsync(string token, string emergencyReason);
        
        // Validate that the QR code belongs to the specified user
        Task<ValidationnResult> ValidateQrCodeOwnershipAsync(string qrCodeData, string userId);
    }
} 