using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace ILLVentApp.Application.Services
{
	public class MedicalHistoryService : IMedicalHistoryService
    {
        private readonly IAppDbContext _context;
        private readonly IQrCodeService _qrCodeService;
        private readonly ILogger<MedicalHistoryService> _logger;
        private readonly string _encryptionKey;

        public MedicalHistoryService(
            IAppDbContext context,
            IQrCodeService qrCodeService,
            ILogger<MedicalHistoryService> logger,
            string encryptionKey)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _encryptionKey = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));
        }

        public async Task<MedicalHistoryResult> SaveMedicalHistoryAsync(SaveMedicalHistoryCommand command, string userId)
        {
            try
            {
                // Validate command
                var validationContext = new ValidationContext(command);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(command, validationContext, validationResults, true))
                {
                    return MedicalHistoryResult.Failed("Validation failed", validationResults.Select(v => v.ErrorMessage).ToList());
                }

                // Check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return MedicalHistoryResult.Failed($"User not found with ID: {userId}");
                }

                // Check if medical history already exists
                var existingHistory = await _context.MedicalHistories
                    .Include(m => m.MedicalConditions)
                    .Include(m => m.FamilyHistory)
                    .Include(m => m.SurgicalHistories)
                    .Include(m => m.ImmunizationHistory)
                    .Include(m => m.SocialHistory)
                    .FirstOrDefaultAsync(m => m.UserId == userId);

                if (existingHistory != null)
                {
                    return await UpdateMedicalHistoryAsync(command, userId);
                }

                try
                {
                    // Create the base medical history object first
                    var medicalHistory = new MedicalHistory
                    {
                        UserId = userId,
                        Address = command.Address,
                        BloodType = command.BloodType,
                        Age = command.Age,
                        Weight = command.Weight,
                        Height = command.Height,
                        Gender = command.Gender,
                        HasHighBloodPressure = command.HasHighBloodPressure,
                        HasLowBloodPressure = command.HasLowBloodPressure,
                        HasDiabetes = command.HasDiabetes,
                        DiabetesType = command.DiabetesType,
                        HasAllergies = command.HasAllergies,
                        AllergiesDetails = command.AllergiesDetails,
                        HasSurgeryHistory = command.HasSurgeryHistory,
                        BirthControlMethod = command.BirthControlMethod,
                        HasBloodTransfusionObjection = command.HasBloodTransfusionObjection
                    };

                    // Generate QR code first
                    try
                    {
                        var qrCodeResult = await GenerateQrCodeAsync(userId);
                        if (!qrCodeResult.Success)
                        {
                            return MedicalHistoryResult.Failed($"Failed to generate QR code: {qrCodeResult.Message}");
                        }

                        medicalHistory.QrCode = qrCodeResult.QrCodeData;
                        medicalHistory.QrCodeGeneratedAt = DateTime.UtcNow;
                        medicalHistory.QrCodeExpiresAt = DateTime.UtcNow.AddYears(1); // QR code valid for 1 year
                    }
                    catch (Exception ex)
                    {
                        return MedicalHistoryResult.Failed($"Error generating QR code: {ex.Message}");
                    }

                    // Add to database first without related entities
                    _context.MedicalHistories.Add(medicalHistory);
                    
                    try
                    {
                        // Ensure all nullable fields have proper null values, not empty strings
                        if (string.IsNullOrEmpty(medicalHistory.DiabetesType))
                            medicalHistory.DiabetesType = null;
                            
                        if (string.IsNullOrEmpty(medicalHistory.AllergiesDetails))
                            medicalHistory.AllergiesDetails = null;
                            
                        if (string.IsNullOrEmpty(medicalHistory.BirthControlMethod))
                            medicalHistory.BirthControlMethod = null;
                            
                        // Save just the medical history entity first
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "Database error when saving basic medical history for user {UserId}", userId);
                        
                        // Log all inner exceptions in detail
                        var innerException = dbEx.InnerException;
                        var errorMessages = new List<string>();
                        
                        while (innerException != null)
                        {
                            errorMessages.Add(innerException.Message);
                            _logger.LogError(innerException, "Inner exception details: {Message}", innerException.Message);
                            innerException = innerException.InnerException;
                        }
                        
                        string errorDetails = string.Join("; ", errorMessages);
                        if (string.IsNullOrEmpty(errorDetails))
                            errorDetails = dbEx.Message;
                            
                        return MedicalHistoryResult.Failed($"Error saving basic medical history: {errorDetails}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving basic medical history: {Message}, Inner: {InnerMessage}", ex.Message, ex.InnerException?.Message);
                        return MedicalHistoryResult.Failed($"Error saving basic medical history: {ex.Message}");
                    }

                    // Now add medical conditions
                    try
                    {
                        // Map medical conditions
                        medicalHistory.MedicalConditions = command.MedicalConditions.Select(mc => new MedicalCondition
                        {
                            MedicalHistoryId = medicalHistory.MedicalHistoryId,
                            Condition = mc.Condition ?? string.Empty,
                            Details = mc.Details ?? string.Empty
                        }).ToList();
                        
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return MedicalHistoryResult.Failed($"Error saving medical conditions: {ex.Message}");
                    }

                    // Now add family history
                    try
                    {
                        // Map family history
                        var familyHistory = new FamilyHistory
                        {
                            MedicalHistoryId = medicalHistory.MedicalHistoryId,
                            HasCancerPolyps = command.FamilyHistory.HasCancerPolyps,
                            CancerPolypsRelationship = command.FamilyHistory.HasCancerPolyps ? command.FamilyHistory.CancerPolypsRelationship : null,
                            HasAnemia = command.FamilyHistory.HasAnemia,
                            AnemiaRelationship = command.FamilyHistory.HasAnemia ? command.FamilyHistory.AnemiaRelationship : null,
                            HasDiabetes = command.FamilyHistory.HasDiabetes,
                            DiabetesRelationship = command.FamilyHistory.HasDiabetes ? command.FamilyHistory.DiabetesRelationship : null,
                            HasBloodClots = command.FamilyHistory.HasBloodClots,
                            BloodClotsRelationship = command.FamilyHistory.HasBloodClots ? command.FamilyHistory.BloodClotsRelationship : null,
                            HasHeartDisease = command.FamilyHistory.HasHeartDisease,
                            HeartDiseaseRelationship = command.FamilyHistory.HasHeartDisease ? command.FamilyHistory.HeartDiseaseRelationship : null,
                            HasStroke = command.FamilyHistory.HasStroke,
                            StrokeRelationship = command.FamilyHistory.HasStroke ? command.FamilyHistory.StrokeRelationship : null,
                            HasHighBloodPressure = command.FamilyHistory.HasHighBloodPressure,
                            HighBloodPressureRelationship = command.FamilyHistory.HasHighBloodPressure ? command.FamilyHistory.HighBloodPressureRelationship : null,
                            HasAnesthesiaReaction = command.FamilyHistory.HasAnesthesiaReaction,
                            AnesthesiaReactionRelationship = command.FamilyHistory.HasAnesthesiaReaction ? command.FamilyHistory.AnesthesiaReactionRelationship : null,
                            HasBleedingProblems = command.FamilyHistory.HasBleedingProblems,
                            BleedingProblemsRelationship = command.FamilyHistory.HasBleedingProblems ? command.FamilyHistory.BleedingProblemsRelationship : null,
                            HasHepatitis = command.FamilyHistory.HasHepatitis,
                            HepatitisRelationship = command.FamilyHistory.HasHepatitis ? command.FamilyHistory.HepatitisRelationship : null,
                            HasOtherCondition = command.FamilyHistory.HasOtherCondition,
                            OtherConditionDetails = command.FamilyHistory.HasOtherCondition ? command.FamilyHistory.OtherConditionDetails : null,
                            OtherConditionRelationship = command.FamilyHistory.HasOtherCondition ? command.FamilyHistory.OtherConditionRelationship : null
                        };
                        
                        medicalHistory.FamilyHistory = new List<FamilyHistory> { familyHistory };
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving family history: {Message}, Inner: {InnerMessage}", ex.Message, ex.InnerException?.Message);
                        return MedicalHistoryResult.Failed($"Error saving family history: {ex.Message}");
                    }

                    // Now add surgical histories
                    try
                    {
                        // Map surgical histories
                        medicalHistory.SurgicalHistories = command.SurgicalHistories.Select(sh => new SurgicalHistory
                        {
                            MedicalHistoryId = medicalHistory.MedicalHistoryId,
                            SurgeryType = sh.SurgeryType ?? string.Empty,
                            Date = sh.Date,
                            Details = sh.Details ?? string.Empty
                        }).ToList();
                        
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return MedicalHistoryResult.Failed($"Error saving surgical histories: {ex.Message}");
                    }

                    // Now add immunization history
                    try
                    {
                        // Map immunization history
                        medicalHistory.ImmunizationHistory = new ImmunizationHistory
                        {
                            MedicalHistoryId = medicalHistory.MedicalHistoryId,
                            HasFlu = command.ImmunizationHistory.HasFlu,
                            FluDate = command.ImmunizationHistory.FluDate,
                            HasTetanus = command.ImmunizationHistory.HasTetanus,
                            TetanusDate = command.ImmunizationHistory.TetanusDate,
                            HasPneumonia = command.ImmunizationHistory.HasPneumonia,
                            PneumoniaDate = command.ImmunizationHistory.PneumoniaDate,
                            HasHepA = command.ImmunizationHistory.HasHepA,
                            HepADate = command.ImmunizationHistory.HepADate,
                            HasHepB = command.ImmunizationHistory.HasHepB,
                            HepBDate = command.ImmunizationHistory.HepBDate
                        };
                        
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return MedicalHistoryResult.Failed($"Error saving immunization history: {ex.Message}");
                    }

                    // Finally add social history
                    try
                    {
                        // Map social history
                        medicalHistory.SocialHistory = new SocialHistory
                        {
                            MedicalHistoryId = medicalHistory.MedicalHistoryId,
                            ExerciseType = string.IsNullOrEmpty(command.SocialHistory.ExerciseType) ? null : command.SocialHistory.ExerciseType,
                            ExerciseFrequency = string.IsNullOrEmpty(command.SocialHistory.ExerciseFrequency) ? null : command.SocialHistory.ExerciseFrequency,
                            PacksPerDay = command.SocialHistory.PacksPerDay,
                            YearsSmoked = command.SocialHistory.YearsSmoked,
                            // Only set YearStopped if there's a smoking history
                            YearStopped = (command.SocialHistory.YearsSmoked > 0) ? command.SocialHistory.YearStopped : null
                        };
                        
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving social history: {Message}, Inner: {InnerMessage}", ex.Message, ex.InnerException?.Message);
                        return MedicalHistoryResult.Failed($"Error saving social history: {ex.Message}");
                    }

                    return MedicalHistoryResult.Successful(MapToMedicalHistoryData(medicalHistory));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during medical history data mapping for user {UserId}", userId);
                    return MedicalHistoryResult.Failed($"Error mapping medical history data: {ex.Message}");
                }
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException;
                var errorMessages = new List<string>();
                
                while (innerException != null)
                {
                    errorMessages.Add(innerException.Message);
                    _logger.LogError(innerException, "Inner exception: {Message}", innerException.Message);
                    innerException = innerException.InnerException;
                }
                
                _logger.LogError(dbEx, "Database error saving medical history for user {UserId}, InnerExceptions: {Errors}", userId, string.Join("; ", errorMessages));
                
                // Try to extract SQL error details if available
                string errorDetails = dbEx.InnerException?.Message ?? dbEx.Message;
                
                return MedicalHistoryResult.Failed($"Database error: {errorDetails}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving medical history for user {UserId}", userId);
                return MedicalHistoryResult.Failed($"An error occurred while saving medical history: {ex.Message}");
            }
        }

        public async Task<MedicalHistoryResult> UpdateMedicalHistoryAsync(SaveMedicalHistoryCommand command, string userId)
        {
            try
            {
                // Validate command
                var validationContext = new ValidationContext(command);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(command, validationContext, validationResults, true))
                {
                    return MedicalHistoryResult.Failed("Validation failed", validationResults.Select(v => v.ErrorMessage).ToList());
                }

                // Get existing medical history
                var medicalHistory = await _context.MedicalHistories
                    .Include(m => m.MedicalConditions)
                    .Include(m => m.FamilyHistory)
                    .Include(m => m.SurgicalHistories)
                    .Include(m => m.ImmunizationHistory)
                    .Include(m => m.SocialHistory)
                    .FirstOrDefaultAsync(m => m.UserId == userId);

                if (medicalHistory == null)
                {
                    return MedicalHistoryResult.Failed("Medical history not found");
                }

                // Update basic info
                medicalHistory.Address = command.Address;
                medicalHistory.BloodType = command.BloodType;
                medicalHistory.Age = command.Age;
                medicalHistory.Weight = command.Weight;
                medicalHistory.Height = command.Height;
                medicalHistory.Gender = command.Gender;
                medicalHistory.HasHighBloodPressure = command.HasHighBloodPressure;
                medicalHistory.HasLowBloodPressure = command.HasLowBloodPressure;
                medicalHistory.HasDiabetes = command.HasDiabetes;
                medicalHistory.DiabetesType = command.DiabetesType;
                medicalHistory.HasAllergies = command.HasAllergies;
                medicalHistory.AllergiesDetails = command.AllergiesDetails;
                medicalHistory.HasSurgeryHistory = command.HasSurgeryHistory;
                medicalHistory.BirthControlMethod = command.BirthControlMethod;
                medicalHistory.HasBloodTransfusionObjection = command.HasBloodTransfusionObjection;

                // Update medical conditions
                _context.MedicalConditions.RemoveRange(medicalHistory.MedicalConditions);
                medicalHistory.MedicalConditions = command.MedicalConditions.Select(mc => new MedicalCondition
                {
                    Condition = mc.Condition,
                    Details = mc.Details
                }).ToList();

                // Update family history
                _context.FamilyHistories.RemoveRange(medicalHistory.FamilyHistory);
                medicalHistory.FamilyHistory = new List<FamilyHistory>
                {
                    new FamilyHistory
                    {
                        HasCancerPolyps = command.FamilyHistory.HasCancerPolyps,
                        CancerPolypsRelationship = command.FamilyHistory.HasCancerPolyps ? command.FamilyHistory.CancerPolypsRelationship : null,
                        HasAnemia = command.FamilyHistory.HasAnemia,
                        AnemiaRelationship = command.FamilyHistory.HasAnemia ? command.FamilyHistory.AnemiaRelationship : null,
                        HasDiabetes = command.FamilyHistory.HasDiabetes,
                        DiabetesRelationship = command.FamilyHistory.HasDiabetes ? command.FamilyHistory.DiabetesRelationship : null,
                        HasBloodClots = command.FamilyHistory.HasBloodClots,
                        BloodClotsRelationship = command.FamilyHistory.HasBloodClots ? command.FamilyHistory.BloodClotsRelationship : null,
                        HasHeartDisease = command.FamilyHistory.HasHeartDisease,
                        HeartDiseaseRelationship = command.FamilyHistory.HasHeartDisease ? command.FamilyHistory.HeartDiseaseRelationship : null,
                        HasStroke = command.FamilyHistory.HasStroke,
                        StrokeRelationship = command.FamilyHistory.HasStroke ? command.FamilyHistory.StrokeRelationship : null,
                        HasHighBloodPressure = command.FamilyHistory.HasHighBloodPressure,
                        HighBloodPressureRelationship = command.FamilyHistory.HasHighBloodPressure ? command.FamilyHistory.HighBloodPressureRelationship : null,
                        HasAnesthesiaReaction = command.FamilyHistory.HasAnesthesiaReaction,
                        AnesthesiaReactionRelationship = command.FamilyHistory.HasAnesthesiaReaction ? command.FamilyHistory.AnesthesiaReactionRelationship : null,
                        HasBleedingProblems = command.FamilyHistory.HasBleedingProblems,
                        BleedingProblemsRelationship = command.FamilyHistory.HasBleedingProblems ? command.FamilyHistory.BleedingProblemsRelationship : null,
                        HasHepatitis = command.FamilyHistory.HasHepatitis,
                        HepatitisRelationship = command.FamilyHistory.HasHepatitis ? command.FamilyHistory.HepatitisRelationship : null,
                        HasOtherCondition = command.FamilyHistory.HasOtherCondition,
                        OtherConditionDetails = command.FamilyHistory.HasOtherCondition ? command.FamilyHistory.OtherConditionDetails : null,
                        OtherConditionRelationship = command.FamilyHistory.HasOtherCondition ? command.FamilyHistory.OtherConditionRelationship : null
                    }
                };

                // Update surgical histories
                _context.SurgicalHistories.RemoveRange(medicalHistory.SurgicalHistories);
                medicalHistory.SurgicalHistories = command.SurgicalHistories.Select(sh => new SurgicalHistory
                {
                    SurgeryType = sh.SurgeryType,
                    Date = sh.Date,
                    Details = sh.Details
                }).ToList();

                // Update immunization history
                if (medicalHistory.ImmunizationHistory == null)
                {
                    medicalHistory.ImmunizationHistory = new ImmunizationHistory();
                }
                medicalHistory.ImmunizationHistory.HasFlu = command.ImmunizationHistory.HasFlu;
                medicalHistory.ImmunizationHistory.FluDate = command.ImmunizationHistory.FluDate;
                medicalHistory.ImmunizationHistory.HasTetanus = command.ImmunizationHistory.HasTetanus;
                medicalHistory.ImmunizationHistory.TetanusDate = command.ImmunizationHistory.TetanusDate;
                medicalHistory.ImmunizationHistory.HasPneumonia = command.ImmunizationHistory.HasPneumonia;
                medicalHistory.ImmunizationHistory.PneumoniaDate = command.ImmunizationHistory.PneumoniaDate;
                medicalHistory.ImmunizationHistory.HasHepA = command.ImmunizationHistory.HasHepA;
                medicalHistory.ImmunizationHistory.HepADate = command.ImmunizationHistory.HepADate;
                medicalHistory.ImmunizationHistory.HasHepB = command.ImmunizationHistory.HasHepB;
                medicalHistory.ImmunizationHistory.HepBDate = command.ImmunizationHistory.HepBDate;

                // Update social history
                if (medicalHistory.SocialHistory == null)
                {
                    medicalHistory.SocialHistory = new SocialHistory();
                }
                medicalHistory.SocialHistory.ExerciseType = string.IsNullOrEmpty(command.SocialHistory.ExerciseType) ? null : command.SocialHistory.ExerciseType;
                medicalHistory.SocialHistory.ExerciseFrequency = string.IsNullOrEmpty(command.SocialHistory.ExerciseFrequency) ? null : command.SocialHistory.ExerciseFrequency;
                medicalHistory.SocialHistory.PacksPerDay = command.SocialHistory.PacksPerDay;
                medicalHistory.SocialHistory.YearsSmoked = command.SocialHistory.YearsSmoked;
                medicalHistory.SocialHistory.YearStopped = (command.SocialHistory.YearsSmoked > 0) ? command.SocialHistory.YearStopped : null;

                // Save changes
                await _context.SaveChangesAsync();

                return MedicalHistoryResult.Successful(MapToMedicalHistoryData(medicalHistory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating medical history for user {UserId}", userId);
                return MedicalHistoryResult.Failed("An error occurred while updating medical history");
            }
        }

        public async Task<MedicalHistoryResult> GetMedicalHistoryByUserIdAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve medical history for user {UserId}", userId);
                
                var medicalHistory = await _context.MedicalHistories
                    .Include(m => m.MedicalConditions)
                    .Include(m => m.FamilyHistory)
                    .Include(m => m.SurgicalHistories)
                    .Include(m => m.ImmunizationHistory)
                    .Include(m => m.SocialHistory)
                    .FirstOrDefaultAsync(m => m.UserId == userId);

                if (medicalHistory == null)
                {
                    _logger.LogWarning("No medical history found for user {UserId}", userId);
                    
                    // Debug: Check if user exists
                    var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                    _logger.LogInformation("User {UserId} exists in database: {Exists}", userId, userExists);
                    
                    return MedicalHistoryResult.Failed("Medical history not found");
                }

                _logger.LogInformation("Successfully found medical history for user {UserId}", userId);
                return MedicalHistoryResult.Successful(MapToMedicalHistoryData(medicalHistory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medical history for user {UserId}", userId);
                return MedicalHistoryResult.Failed("An error occurred while retrieving medical history");
            }
        }

        public async Task<QrCodeResult> GenerateQrCodeAsync(string userId)
        {
            try
            {
                // Check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return QrCodeResult.Failed("User not found");
                }

                // Generate a persistent token for the user if it doesn't exist
                if (string.IsNullOrEmpty(user.QrCode))
                {
                    user.QrCode = GeneratePersistentToken(userId);
                    
                    // Save the change to the database
                    await _context.SaveChangesAsync();
                    
                    // Log that we created a new token
                    _logger.LogInformation("Generated new persistent token for user {UserId}: {Token}", userId, user.QrCode);
                }
                else
                {
                    // Log the existing token
                    _logger.LogInformation("Using existing token for user {UserId}: {Token}", userId, user.QrCode);
                }

                // Generate a user-friendly version of the token (first 8 chars)
                string userFriendlyToken = user.QrCode.Substring(0, 8).ToUpper();
                
                try
                {
                    // Generate QR code with the persistent token (NOT storing the QR code in user.QrCode)
                    var qrCode = await _qrCodeService.GenerateQrCodeAsync(user.QrCode);
                    return QrCodeResult.Successful(qrCode, userFriendlyToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating QR code image for user {UserId}", userId);
                    return QrCodeResult.Failed($"Error generating QR code image: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for user {UserId}", userId);
                return QrCodeResult.Failed("An error occurred while generating QR code");
            }
        }

        public async Task<MedicalHistoryResult> GetMedicalHistoryByQrCodeAsync(string qrCodeData)
        {
            try
            {
                // Decode QR code data
                var decodedData = await _qrCodeService.DecodeQrCodeAsync(qrCodeData);
                if (string.IsNullOrEmpty(decodedData))
                {
                    return MedicalHistoryResult.Failed("Invalid QR code data");
                }

                // Find user by QR code token
                var user = await _context.Users.FirstOrDefaultAsync(u => u.QrCode == decodedData);
                if (user == null)
                {
                    return MedicalHistoryResult.Failed("User not found");
                }

                // Get medical history
                return await GetMedicalHistoryByUserIdAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medical history for QR code");
                return MedicalHistoryResult.Failed("An error occurred while retrieving medical history");
            }
        }

        public async Task<MedicalHistoryResult> GetMedicalHistoryByQrCodeAsync(string qrCodeData, string userId)
        {
            try
            {
                _logger.LogInformation("Secure QR code access attempt by user {UserId}", userId);
                
                string encryptedText = null;
                
                // Check if this is a base64 image (PNG or other image format)
                if (qrCodeData.StartsWith("iVBORw0KGgo") || qrCodeData.StartsWith("data:image") || qrCodeData.Length > 1000)
                {
                    _logger.LogInformation("Secure access with QR code image detected, attempting to read...");
                    
                    try
                    {
                        // Use the new method to read the QR code image and extract encrypted text
                        encryptedText = await _qrCodeService.ReadQrCodeImageAsync(qrCodeData);
                        
                        if (string.IsNullOrEmpty(encryptedText))
                        {
                            _logger.LogWarning("No QR code content found in image for user {UserId}", userId);
                            return MedicalHistoryResult.Failed("No readable QR code found in the provided image.");
                        }
                        
                        _logger.LogInformation("Successfully extracted encrypted text from QR code image. Length: {Length}", encryptedText.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to read QR code image in secure access for user {UserId}", userId);
                        return MedicalHistoryResult.Failed($"Failed to read QR code image: {ex.Message}");
                    }
                }
                else
                {
                    // This should be encrypted text directly from a QR code
                    encryptedText = qrCodeData;
                    _logger.LogInformation("Secure access with encrypted text directly, length: {Length}", encryptedText.Length);
                }
                
                // Validate ownership first using the encrypted text
                var validationResult = await ValidateQrCodeOwnershipAsync(encryptedText, userId);
                if (!validationResult.Success)
                {
                    _logger.LogWarning("User {UserId} failed ownership validation for secure QR code access: {Message}", userId, validationResult.Message);
                    return MedicalHistoryResult.Failed("You are not authorized to access this QR code");
                }
                
                // Now decrypt the encrypted text to get the user token and retrieve medical history
                try
                {
                    var decodedData = await _qrCodeService.DecodeQrCodeAsync(encryptedText);
                    if (string.IsNullOrEmpty(decodedData))
                    {
                        return MedicalHistoryResult.Failed("Invalid QR code data");
                    }

                    // Find user by QR code token
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.QrCode == decodedData);
                    if (user == null)
                    {
                        return MedicalHistoryResult.Failed("User not found");
                    }

                    // Get medical history (full data for secure access)
                    _logger.LogInformation("Successfully validated ownership and retrieving medical history for user {UserId}", userId);
                    return await GetMedicalHistoryByUserIdAsync(user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt QR code data in secure access for user {UserId}", userId);
                    return MedicalHistoryResult.Failed("Invalid QR code format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in secure QR code access for user {UserId}", userId);
                return MedicalHistoryResult.Failed("An error occurred while retrieving medical history");
            }
        }

        public async Task<MedicalHistoryResult> GetMedicalHistoryByTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return MedicalHistoryResult.Failed("Invalid token");
                }

                // Clean and normalize the token
                token = token.Trim().ToUpper();
                _logger.LogInformation("Looking for users with token: {Token}", token);

                // Get all users with QR codes first, then filter in memory to avoid EF Core translation issues
                var allUsers = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.QrCode))
                    .ToListAsync();

                // Filter in memory using case-insensitive comparison
                var matchingUsers = allUsers.Where(u => 
                    u.QrCode.StartsWith(token, StringComparison.OrdinalIgnoreCase) || 
                    (u.QrCode.Length >= 8 && u.QrCode.Substring(0, 8).Equals(token, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                _logger.LogInformation("Found {Count} users matching token {Token}", matchingUsers.Count, token);

                if (!matchingUsers.Any())
                {
                    // Log some sample QR codes for debugging
                    var qrCodeSamples = allUsers
                        .Take(5)
                        .Select(u => new { UserId = u.Id, QrCodePrefix = u.QrCode.Length >= 8 ? u.QrCode.Substring(0, 8) : u.QrCode })
                        .ToList();
                        
                    _logger.LogWarning("No users found for token {Token}. Sample QR codes: {@QrCodeSamples}", 
                        token, qrCodeSamples);

                    return MedicalHistoryResult.Failed("User not found for this token");
                }

                if (matchingUsers.Count > 1)
                {
                    // This should be rare but handle it for security
                    _logger.LogWarning("Multiple users found for token {Token}", token);
                    return MedicalHistoryResult.Failed("Ambiguous token, please use the QR code instead");
                }

                // Get medical history for the single user
                _logger.LogInformation("Found matching user {UserId} for token {Token}", matchingUsers.First().Id, token);
                return await GetMedicalHistoryByUserIdAsync(matchingUsers.First().Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medical history for token");
                return MedicalHistoryResult.Failed("An error occurred while retrieving medical history");
            }
        }

        public async Task<ValidationnResult> ValidateQrCodeOwnershipAsync(string qrCodeData, string userId)
        {
            try
            {
                // Check for token-based validation
                if (qrCodeData.StartsWith("TOKEN:"))
                {
                    string token = qrCodeData.Substring(6); // Remove the "TOKEN:" prefix
                    
                    // Clean and normalize the token
                    token = token.Trim().ToUpper();
                    _logger.LogInformation("Validating token: {Token} for user {UserId}", token, userId);

                    // Find users whose QR code starts with the token (case-insensitive)
                    var allUsers = await _context.Users.ToListAsync();
                    _logger.LogInformation("Total users in database: {Count}", allUsers.Count);
                    
                    // Debug: Log the specific user we're validating for
                    var currentUser = allUsers.FirstOrDefault(u => u.Id == userId);
                    if (currentUser != null)
                    {
                        _logger.LogInformation("Current user {UserId} has QrCode: {QrCode}", userId, currentUser.QrCode ?? "NULL");
                        if (!string.IsNullOrEmpty(currentUser.QrCode))
                        {
                            var userTokenPrefix = currentUser.QrCode.Length >= 8 ? currentUser.QrCode.Substring(0, 8).ToUpper() : currentUser.QrCode.ToUpper();
                            _logger.LogInformation("Current user token prefix: {Prefix}, Looking for: {Token}", userTokenPrefix, token);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Current user {UserId} not found in database!", userId);
                    }
                    
                    // Filter in memory to avoid EF Core translation issues
                    var matchingUsers = allUsers.Where(u => 
                        !string.IsNullOrEmpty(u.QrCode) && 
                        (u.QrCode.StartsWith(token, StringComparison.OrdinalIgnoreCase) || 
                         (u.QrCode.Length >= 8 && u.QrCode.Substring(0, 8).Equals(token, StringComparison.OrdinalIgnoreCase)))
                    ).ToList();
                    
                    _logger.LogInformation("Found {Count} users matching token {Token}", matchingUsers.Count, token);
                    
                    // Debug: Log details about matching users
                    foreach (var matchingUser in matchingUsers)
                    {
                        _logger.LogInformation("Matching user: {UserId}, QrCode: {QrCode}", matchingUser.Id, matchingUser.QrCode);
                    }
                    
                    if (matchingUsers.Count == 0)
                    {
                        // Log the first few characters of each user's QR code for debugging
                        var qrCodeSamples = allUsers
                            .Where(u => !string.IsNullOrEmpty(u.QrCode))
                            .Take(5)
                            .Select(u => new { UserId = u.Id, QrCodePrefix = u.QrCode.Length > 8 ? u.QrCode.Substring(0, 8) : u.QrCode })
                            .ToList();
                            
                        _logger.LogWarning("No users found for token {Token}. Sample QR codes: {@QrCodeSamples}", 
                            token, qrCodeSamples);
                            
                        return ValidationnResult.Failed("User not found for this token");
                    }

                    if (matchingUsers.Count > 1)
                    {
                        _logger.LogWarning("Multiple users found for token {Token}: {@UserIds}", 
                            token, matchingUsers.Select(u => u.Id));
                            
                        return ValidationnResult.Failed("Ambiguous token, please use the QR code instead");
                    }

                    var user = matchingUsers.First();
                    
                    // Verify ownership - make sure the authenticated user ID matches the token owner
                    if (user.Id != userId)
                    {
                        _logger.LogWarning("Unauthorized token access attempt: User {UserId} attempted to access token for user {OwnerId}", userId, user.Id);
                        return ValidationnResult.Failed("You are not authorized to access this token");
                    }

                    _logger.LogInformation("Token validation successful for user {UserId}", userId);
                    return ValidationnResult.Successful();
                }
                
                // Regular QR code validation
                var decodedData = await _qrCodeService.DecodeQrCodeAsync(qrCodeData);
                if (string.IsNullOrEmpty(decodedData))
                {
                    return ValidationnResult.Failed("Invalid QR code data");
                }

                // Find user by QR code token
                var qrUser = await _context.Users.FirstOrDefaultAsync(u => u.QrCode == decodedData);
                if (qrUser == null)
                {
                    return ValidationnResult.Failed("User not found for this QR code");
                }

                // Verify ownership - make sure the authenticated user ID matches the QR code owner
                if (qrUser.Id != userId)
                {
                    _logger.LogWarning("Unauthorized QR code access attempt: User {UserId} attempted to access QR code for user {OwnerId}", userId, qrUser.Id);
                    return ValidationnResult.Failed("You are not authorized to access this QR code");
                }

                return ValidationnResult.Successful();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code ownership for user {UserId}", userId);
                return ValidationnResult.Failed("An error occurred while validating QR code ownership");
            }
        }

        public async Task<MedicalHistoryResult> GetEmergencyMedicalHistoryByQrCodeAsync(string qrCodeData, string emergencyReason)
        {
            try
            {
                string encryptedText = null;
                
                // Check if this is a base64 image (PNG or other image format)
                if (qrCodeData.StartsWith("iVBORw0KGgo") || qrCodeData.StartsWith("data:image") || qrCodeData.Length > 1000)
                {
                    _logger.LogInformation("Emergency access with QR code image detected, attempting to read...");
                    
                    try
                    {
                        // Use the new method to read the QR code image and extract encrypted text
                        encryptedText = await _qrCodeService.ReadQrCodeImageAsync(qrCodeData);
                        
                        if (string.IsNullOrEmpty(encryptedText))
                        {
                            _logger.LogWarning("No QR code content found in image");
                            return MedicalHistoryResult.Failed("No readable QR code found in the provided image. Please ensure the image contains a valid QR code or use the 8-character token with the /emergency/token/{token} endpoint instead.");
                        }
                        
                        _logger.LogInformation("Successfully extracted encrypted text from QR code image. Length: {Length}", encryptedText.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to read QR code image in emergency access");
                        return MedicalHistoryResult.Failed($"Failed to read QR code image: {ex.Message}. Please use the 8-character token with the /emergency/token/{{token}} endpoint instead.");
                    }
                }
                else
                {
                    // This should be encrypted text directly from a QR code
                    encryptedText = qrCodeData;
                    _logger.LogInformation("Emergency access with encrypted text directly, length: {Length}", encryptedText.Length);
                }
                
                // Now decrypt the encrypted text to get the user token
                string decodedData = null;
                try
                {
                    decodedData = await _qrCodeService.DecodeQrCodeAsync(encryptedText);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt QR code data in emergency access");
                    return MedicalHistoryResult.Failed("Invalid QR code format. Please ensure you're using a valid QR code or use the 8-character token with the /emergency/token/{token} endpoint instead.");
                }
                
                if (string.IsNullOrEmpty(decodedData))
                {
                    return MedicalHistoryResult.Failed("Invalid QR code data. Please ensure you're using a valid QR code or use the 8-character token with the /emergency/token/{token} endpoint instead.");
                }

                // Find user by QR code token
                var user = await _context.Users.FirstOrDefaultAsync(u => u.QrCode == decodedData);
                if (user == null)
                {
                    return MedicalHistoryResult.Failed("User not found. Please verify the QR code or use the 8-character token with the /emergency/token/{token} endpoint instead.");
                }

                // Log the emergency access attempt for audit trail
                _logger.LogWarning("EMERGENCY ACCESS: QR code used to access medical history for user {UserId}. Reason: {Reason}", 
                    user.Id, emergencyReason);

                // Get medical history
                var medicalHistoryResult = await GetMedicalHistoryByUserIdAsync(user.Id);
                
                // For emergency access, we want to return a subset of the medical history
                if (medicalHistoryResult.Success)
                {
                    // Create a limited version for emergency purposes
                    var limitedData = new MedicalHistoryData
                    {
                        BloodType = medicalHistoryResult.Data.BloodType,
                        HasAllergies = medicalHistoryResult.Data.HasAllergies,
                        AllergiesDetails = medicalHistoryResult.Data.HasAllergies ? medicalHistoryResult.Data.AllergiesDetails : null,
                        MedicalConditions = medicalHistoryResult.Data.MedicalConditions,
                        HasBloodTransfusionObjection = medicalHistoryResult.Data.HasBloodTransfusionObjection,
                        HasDiabetes = medicalHistoryResult.Data.HasDiabetes,
                        DiabetesType = medicalHistoryResult.Data.HasDiabetes ? medicalHistoryResult.Data.DiabetesType : null,
                        HasHighBloodPressure = medicalHistoryResult.Data.HasHighBloodPressure,
                        HasLowBloodPressure = medicalHistoryResult.Data.HasLowBloodPressure,
                        // Emergency-relevant data only
                    };
                    
                    // Return successful result with limited data
                    return MedicalHistoryResult.Successful(limitedData, "Emergency access granted");
                }
                
                return medicalHistoryResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emergency medical history for QR code");
                return MedicalHistoryResult.Failed("An error occurred while retrieving emergency medical history");
            }
        }

        public async Task<MedicalHistoryResult> GetEmergencyMedicalHistoryByTokenAsync(string token, string emergencyReason)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return MedicalHistoryResult.Failed("Invalid token");
                }

                // Clean and normalize the token (uppercase, remove spaces)
                token = token.Trim().ToUpper();
                _logger.LogInformation("Emergency access attempt using token: {Token}, Reason: {Reason}", token, emergencyReason);

                // Find users whose QR code starts with the token (case-insensitive)
                var allUsers = await _context.Users.ToListAsync();
                var matchingUsers = allUsers.Where(u => 
                    !string.IsNullOrEmpty(u.QrCode) && 
                    (u.QrCode.StartsWith(token, StringComparison.OrdinalIgnoreCase) || 
                     (u.QrCode.Length >= 8 && u.QrCode.Substring(0, 8).Equals(token, StringComparison.OrdinalIgnoreCase)))
                ).ToList();

                if (!matchingUsers.Any())
                {
                    // Log the first few characters of each user's QR code for debugging
                    var qrCodeSamples = allUsers
                        .Where(u => !string.IsNullOrEmpty(u.QrCode))
                        .Take(5)
                        .Select(u => new { UserId = u.Id, QrCodePrefix = u.QrCode.Length > 8 ? u.QrCode.Substring(0, 8) : u.QrCode })
                        .ToList();
                        
                    _logger.LogWarning("No users found for emergency token {Token}. Sample QR codes: {@QrCodeSamples}", 
                        token, qrCodeSamples);
                        
                    return MedicalHistoryResult.Failed("User not found for this token");
                }

                if (matchingUsers.Count > 1)
                {
                    // This should be rare but handle it for security
                    _logger.LogWarning("Multiple users found for token {Token}", token);
                    return MedicalHistoryResult.Failed("Ambiguous token, please use the QR code instead");
                }

                // Log the emergency access attempt for audit trail
                _logger.LogWarning("EMERGENCY ACCESS: Token used to access medical history for user {UserId}. Reason: {Reason}", 
                    matchingUsers.First().Id, emergencyReason);

                // Get medical history for the single user
                var result = await GetMedicalHistoryByUserIdAsync(matchingUsers.First().Id);
                
                // For emergency access, we want to return a subset of the medical history
                if (result.Success)
                {
                    // Create a limited version for emergency purposes
                    var limitedData = new MedicalHistoryData
                    {
                        BloodType = result.Data.BloodType,
                        HasAllergies = result.Data.HasAllergies,
                        AllergiesDetails = result.Data.HasAllergies ? result.Data.AllergiesDetails : null,
                        MedicalConditions = result.Data.MedicalConditions,
                        HasBloodTransfusionObjection = result.Data.HasBloodTransfusionObjection,
                        HasDiabetes = result.Data.HasDiabetes,
                        DiabetesType = result.Data.HasDiabetes ? result.Data.DiabetesType : null,
                        HasHighBloodPressure = result.Data.HasHighBloodPressure,
                        HasLowBloodPressure = result.Data.HasLowBloodPressure,
                        // Emergency-relevant data only
                    };
                    
                    // Return successful result with limited data
                    return MedicalHistoryResult.Successful(limitedData, "Emergency access granted");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emergency medical history for token: {Token}", token);
                return MedicalHistoryResult.Failed("An error occurred while retrieving emergency medical history");
            }
        }

        private string GeneratePersistentToken(string userId)
        {
            // Generate a deterministic but secure token based on user ID
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_encryptionKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(userId));
                
                // Format: Base64 encoding of the hash with a prefix to make it more user-friendly
                // First create the full token (32 characters)
                string fullToken = Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-").Substring(0, 32);
                
                // Store the full token but return a user-friendly version
                return fullToken;
            }
        }

        private MedicalHistoryData MapToMedicalHistoryData(MedicalHistory medicalHistory)
        {
            return new MedicalHistoryData
            {
                // Removed UserId and other internal IDs for security
                Address = medicalHistory.Address,
                BloodType = medicalHistory.BloodType,
                Age = medicalHistory.Age,
                Weight = medicalHistory.Weight,
                Height = medicalHistory.Height,
                Gender = medicalHistory.Gender,
                HasHighBloodPressure = medicalHistory.HasHighBloodPressure,
                HasLowBloodPressure = medicalHistory.HasLowBloodPressure,
                HasDiabetes = medicalHistory.HasDiabetes,
                DiabetesType = medicalHistory.DiabetesType,
                HasAllergies = medicalHistory.HasAllergies,
                AllergiesDetails = medicalHistory.AllergiesDetails,
                HasSurgeryHistory = medicalHistory.HasSurgeryHistory,
                BirthControlMethod = medicalHistory.BirthControlMethod,
                HasBloodTransfusionObjection = medicalHistory.HasBloodTransfusionObjection,
                MedicalConditions = medicalHistory.MedicalConditions.Select(mc => new MedicalConditionDto
                {
                    // Removed internal IDs for security
                    Condition = mc.Condition,
                    Details = mc.Details
                }).ToList(),
                FamilyHistory = medicalHistory.FamilyHistory.FirstOrDefault() != null ? new FamilyHistoryDto
                {
                    // Removed internal IDs for security
                    HasCancerPolyps = medicalHistory.FamilyHistory.First().HasCancerPolyps,
                    CancerPolypsRelationship = medicalHistory.FamilyHistory.First().CancerPolypsRelationship,
                    HasAnemia = medicalHistory.FamilyHistory.First().HasAnemia,
                    AnemiaRelationship = medicalHistory.FamilyHistory.First().AnemiaRelationship,
                    HasDiabetes = medicalHistory.FamilyHistory.First().HasDiabetes,
                    DiabetesRelationship = medicalHistory.FamilyHistory.First().DiabetesRelationship,
                    HasBloodClots = medicalHistory.FamilyHistory.First().HasBloodClots,
                    BloodClotsRelationship = medicalHistory.FamilyHistory.First().BloodClotsRelationship,
                    HasHeartDisease = medicalHistory.FamilyHistory.First().HasHeartDisease,
                    HeartDiseaseRelationship = medicalHistory.FamilyHistory.First().HeartDiseaseRelationship,
                    HasStroke = medicalHistory.FamilyHistory.First().HasStroke,
                    StrokeRelationship = medicalHistory.FamilyHistory.First().StrokeRelationship,
                    HasHighBloodPressure = medicalHistory.FamilyHistory.First().HasHighBloodPressure,
                    HighBloodPressureRelationship = medicalHistory.FamilyHistory.First().HighBloodPressureRelationship,
                    HasAnesthesiaReaction = medicalHistory.FamilyHistory.First().HasAnesthesiaReaction,
                    AnesthesiaReactionRelationship = medicalHistory.FamilyHistory.First().AnesthesiaReactionRelationship,
                    HasBleedingProblems = medicalHistory.FamilyHistory.First().HasBleedingProblems,
                    BleedingProblemsRelationship = medicalHistory.FamilyHistory.First().BleedingProblemsRelationship,
                    HasHepatitis = medicalHistory.FamilyHistory.First().HasHepatitis,
                    HepatitisRelationship = medicalHistory.FamilyHistory.First().HepatitisRelationship,
                    HasOtherCondition = medicalHistory.FamilyHistory.First().HasOtherCondition,
                    OtherConditionDetails = medicalHistory.FamilyHistory.First().OtherConditionDetails,
                    OtherConditionRelationship = medicalHistory.FamilyHistory.First().OtherConditionRelationship
                } : new FamilyHistoryDto(),
                SurgicalHistories = medicalHistory.SurgicalHistories.Select(sh => new SurgicalHistoryDto
                {
                    // Removed internal IDs for security
                    SurgeryType = sh.SurgeryType,
                    Date = sh.Date,
                    Details = sh.Details
                }).ToList(),
                ImmunizationHistory = medicalHistory.ImmunizationHistory != null ? new ImmunizationHistoryDto
                {
                    // Removed internal IDs for security
                    HasFlu = medicalHistory.ImmunizationHistory.HasFlu,
                    FluDate = medicalHistory.ImmunizationHistory.FluDate,
                    HasTetanus = medicalHistory.ImmunizationHistory.HasTetanus,
                    TetanusDate = medicalHistory.ImmunizationHistory.TetanusDate,
                    HasPneumonia = medicalHistory.ImmunizationHistory.HasPneumonia,
                    PneumoniaDate = medicalHistory.ImmunizationHistory.PneumoniaDate,
                    HasHepA = medicalHistory.ImmunizationHistory.HasHepA,
                    HepADate = medicalHistory.ImmunizationHistory.HepADate,
                    HasHepB = medicalHistory.ImmunizationHistory.HasHepB,
                    HepBDate = medicalHistory.ImmunizationHistory.HepBDate
                } : new ImmunizationHistoryDto(),
                SocialHistory = medicalHistory.SocialHistory != null ? new SocialHistoryDto
                {
                    ExerciseType = medicalHistory.SocialHistory.ExerciseType,
                    ExerciseFrequency = medicalHistory.SocialHistory.ExerciseFrequency,
                    PacksPerDay = medicalHistory.SocialHistory.PacksPerDay,
                    YearsSmoked = medicalHistory.SocialHistory.YearsSmoked,
                    YearStopped = medicalHistory.SocialHistory.YearStopped
                } : new SocialHistoryDto(),
                QrCode = medicalHistory.QrCode,
                QrCodeGeneratedAt = medicalHistory.QrCodeGeneratedAt,
                QrCodeExpiresAt = medicalHistory.QrCodeExpiresAt
            };
        }
    }
} 