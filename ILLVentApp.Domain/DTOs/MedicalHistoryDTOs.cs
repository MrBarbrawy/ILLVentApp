using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ILLVentApp.Domain.Models;
using org.omg.CosNaming.NamingContextPackage;

namespace ILLVentApp.Domain.DTOs
{
    // Command to save medical history
    public class SaveMedicalHistoryCommand : IValidatableObject
    {
        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Blood type is required")]
        [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Invalid blood type format (e.g., A+, B-, AB+, O-)")]
        public string BloodType { get; set; }

        [Required(ErrorMessage = "Age is required")]
        [Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Weight is required")]
        [Range(0, 500, ErrorMessage = "Weight must be between 0 and 500 kg")]
        public decimal Weight { get; set; }

        [Required(ErrorMessage = "Height is required")]
        [Range(0, 300, ErrorMessage = "Height must be between 0 and 300 cm")]
        public decimal Height { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression(@"^(Male|Female)$", ErrorMessage = "Gender must be either Male or Female")]
        public string Gender { get; set; }

        public bool HasHighBloodPressure { get; set; }
        public bool HasLowBloodPressure { get; set; }
        public bool HasDiabetes { get; set; }
        
        [RequiredIf(nameof(HasDiabetes), true, ErrorMessage = "Diabetes type is required when diabetes is indicated")]
        [RegularExpression(@"^(Type 1 Diabetes|Type 2 Diabetes|Gestational Diabetes|Prediabetes)$", 
            ErrorMessage = "Invalid diabetes type")]
        public string DiabetesType { get; set; }

        [Required(ErrorMessage = "Medical conditions are required")]
        public List<MedicalConditionDto> MedicalConditions { get; set; } = new();
        
        public bool HasAllergies { get; set; }
        [RequiredIf(nameof(HasAllergies), true, ErrorMessage = "Allergy details are required when allergies are indicated")]
        [StringLength(500, ErrorMessage = "Allergies details cannot exceed 500 characters")]
        public string AllergiesDetails { get; set; }

        [Required(ErrorMessage = "Family history is required")]
        public FamilyHistoryDto FamilyHistory { get; set; } = new FamilyHistoryDto();

        public bool HasSurgeryHistory { get; set; }
        [RequiredIf(nameof(HasSurgeryHistory), true, ErrorMessage = "At least one surgery record is required when surgery history is indicated")]
        public List<SurgicalHistoryDto> SurgicalHistories { get; set; } = new();

        [StringLength(100, ErrorMessage = "Birth control method cannot exceed 100 characters")]
        public string BirthControlMethod { get; set; }
        public bool HasBloodTransfusionObjection { get; set; }

        [Required(ErrorMessage = "Immunization history is required")]
        public ImmunizationHistoryDto ImmunizationHistory { get; set; }

        [Required(ErrorMessage = "Social history is required")]
        public SocialHistoryDto SocialHistory { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Cannot have both high and low blood pressure
            if (HasHighBloodPressure && HasLowBloodPressure)
            {
                results.Add(new ValidationResult(
                    "A person cannot have both high and low blood pressure simultaneously",
                    new[] { nameof(HasHighBloodPressure), nameof(HasLowBloodPressure) }));
            }

            // Validate medical conditions
            if (MedicalConditions?.Count > 0)
            {
                foreach (var condition in MedicalConditions)
                {
                    if (string.IsNullOrWhiteSpace(condition.Condition))
                    {
                        results.Add(new ValidationResult(
                            "Medical condition cannot be empty",
                            new[] { nameof(MedicalConditions) }));
                        break;
                    }
                }
            }

            // Validate family history relationships
            if (FamilyHistory != null)
            {
                if (FamilyHistory.HasCancerPolyps && string.IsNullOrWhiteSpace(FamilyHistory.CancerPolypsRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for Cancer/Polyps"));

                if (FamilyHistory.HasAnemia && string.IsNullOrWhiteSpace(FamilyHistory.AnemiaRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for Anemia"));

                if (FamilyHistory.HasDiabetes && string.IsNullOrWhiteSpace(FamilyHistory.DiabetesRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for Diabetes"));

                if (FamilyHistory.HasBloodClots && string.IsNullOrWhiteSpace(FamilyHistory.BloodClotsRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for Blood Clots"));

                if (FamilyHistory.HasHeartDisease && string.IsNullOrWhiteSpace(FamilyHistory.HeartDiseaseRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for Heart Disease"));

                if (FamilyHistory.HasStroke && string.IsNullOrWhiteSpace(FamilyHistory.StrokeRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for Stroke"));

                if (FamilyHistory.HasHighBloodPressure && string.IsNullOrWhiteSpace(FamilyHistory.HighBloodPressureRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for High Blood Pressure"));

                if (FamilyHistory.HasAnesthesiaReaction && string.IsNullOrWhiteSpace(FamilyHistory.AnesthesiaReactionRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for Anesthesia Reaction"));

                if (FamilyHistory.HasBleedingProblems && string.IsNullOrWhiteSpace(FamilyHistory.BleedingProblemsRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for Bleeding Problems"));

                if (FamilyHistory.HasHepatitis && string.IsNullOrWhiteSpace(FamilyHistory.HepatitisRelationship))
                    results.Add(new ValidationResult("Please specify the relationship for Hepatitis"));

                if (FamilyHistory.HasOtherCondition)
                {
                    if (string.IsNullOrWhiteSpace(FamilyHistory.OtherConditionDetails))
                        results.Add(new ValidationResult("Please specify the other condition"));
                    if (string.IsNullOrWhiteSpace(FamilyHistory.OtherConditionRelationship))
                        results.Add(new ValidationResult("Please specify the relationship for the other condition"));
                }
            }

            // Validate surgical history
            if (HasSurgeryHistory && (SurgicalHistories == null || SurgicalHistories.Count == 0))
            {
                results.Add(new ValidationResult(
                    "At least one surgery record is required when surgery history is indicated",
                    new[] { nameof(SurgicalHistories) }));
            }

            // Validate immunization dates
            if (ImmunizationHistory != null)
            {
                if (ImmunizationHistory.HasFlu && !ImmunizationHistory.FluDate.HasValue)
                    results.Add(new ValidationResult("Flu shot date is required when flu shot is indicated"));

                if (ImmunizationHistory.HasTetanus && !ImmunizationHistory.TetanusDate.HasValue)
                    results.Add(new ValidationResult("Tetanus shot date is required when tetanus shot is indicated"));

                if (ImmunizationHistory.HasPneumonia && !ImmunizationHistory.PneumoniaDate.HasValue)
                    results.Add(new ValidationResult("Pneumonia shot date is required when pneumonia shot is indicated"));

                if (ImmunizationHistory.HasHepA && !ImmunizationHistory.HepADate.HasValue)
                    results.Add(new ValidationResult("Hepatitis A shot date is required when Hepatitis A shot is indicated"));

                if (ImmunizationHistory.HasHepB && !ImmunizationHistory.HepBDate.HasValue)
                    results.Add(new ValidationResult("Hepatitis B shot date is required when Hepatitis B shot is indicated"));
            }

            // Validate allergies
            if (HasAllergies && string.IsNullOrWhiteSpace(AllergiesDetails))
            {
                results.Add(new ValidationResult(
                    "Please specify your allergies",
                    new[] { nameof(AllergiesDetails) }));
            }

            return results;
        }
    }

    // Custom validation attribute for conditional required fields
    public class RequiredIfAttribute : ValidationAttribute
    {
        private string PropertyName { get; set; }
        private object DesiredValue { get; set; }

        public RequiredIfAttribute(string propertyName, object desiredValue)
        {
            PropertyName = propertyName;
            DesiredValue = desiredValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();
            var propertyValue = type.GetProperty(PropertyName)?.GetValue(instance, null);

            if (propertyValue?.ToString() == DesiredValue.ToString() && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
    
    public class MedicalConditionDto
    {
        public int Id { get; set; }
        public int MedicalHistoryId { get; set; }

        [Required(ErrorMessage = "Condition is required")]
        [StringLength(100, ErrorMessage = "Condition cannot exceed 100 characters")]
        public string Condition { get; set; }

        [Required(ErrorMessage = "Details are required")]
        [StringLength(500, ErrorMessage = "Details cannot exceed 500 characters")]
        public string Details { get; set; }
    }
    
    public class FamilyHistoryDto
    {
        public int Id { get; set; }
        public int MedicalHistoryId { get; set; }

        public bool HasCancerPolyps { get; set; }
        [RequiredIf(nameof(HasCancerPolyps), true, ErrorMessage = "Relationship is required when Cancer/Polyp is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string CancerPolypsRelationship { get; set; }

        public bool HasAnemia { get; set; }
        [RequiredIf(nameof(HasAnemia), true, ErrorMessage = "Relationship is required when Anemia is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string AnemiaRelationship { get; set; }

        public bool HasDiabetes { get; set; }
        [RequiredIf(nameof(HasDiabetes), true, ErrorMessage = "Relationship is required when Diabetes is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string DiabetesRelationship { get; set; }

        public bool HasBloodClots { get; set; }
        [RequiredIf(nameof(HasBloodClots), true, ErrorMessage = "Relationship is required when Blood Clots is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string BloodClotsRelationship { get; set; }

        public bool HasHeartDisease { get; set; }
        [RequiredIf(nameof(HasHeartDisease), true, ErrorMessage = "Relationship is required when Heart Disease is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string HeartDiseaseRelationship { get; set; }

        public bool HasStroke { get; set; }
        [RequiredIf(nameof(HasStroke), true, ErrorMessage = "Relationship is required when Stroke is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string StrokeRelationship { get; set; }

        public bool HasHighBloodPressure { get; set; }
        [RequiredIf(nameof(HasHighBloodPressure), true, ErrorMessage = "Relationship is required when High Blood Pressure is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string HighBloodPressureRelationship { get; set; }

        public bool HasAnesthesiaReaction { get; set; }
        [RequiredIf(nameof(HasAnesthesiaReaction), true, ErrorMessage = "Relationship is required when Anesthesia Reaction is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string AnesthesiaReactionRelationship { get; set; }

        public bool HasBleedingProblems { get; set; }
        [RequiredIf(nameof(HasBleedingProblems), true, ErrorMessage = "Relationship is required when Bleeding Problems is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string BleedingProblemsRelationship { get; set; }

        public bool HasHepatitis { get; set; }
        [RequiredIf(nameof(HasHepatitis), true, ErrorMessage = "Relationship is required when Hepatitis is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string HepatitisRelationship { get; set; }

        public bool HasOtherCondition { get; set; }
        [RequiredIf(nameof(HasOtherCondition), true, ErrorMessage = "Details are required when Other Condition is selected")]
        [StringLength(100, ErrorMessage = "Other condition cannot exceed 100 characters")]
        public string OtherConditionDetails { get; set; }
        [RequiredIf(nameof(HasOtherCondition), true, ErrorMessage = "Relationship is required when Other Condition is selected")]
        [StringLength(50, ErrorMessage = "Relationship cannot exceed 50 characters")]
        public string OtherConditionRelationship { get; set; }
    }
    
    public class SurgicalHistoryDto
    {
        public int Id { get; set; }
        public int MedicalHistoryId { get; set; }

        [Required(ErrorMessage = "Surgery type is required")]
        [StringLength(100, ErrorMessage = "Surgery type cannot exceed 100 characters")]
        public string SurgeryType { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Details are required")]
        [StringLength(500, ErrorMessage = "Details cannot exceed 500 characters")]
        public string Details { get; set; }
    }
    
    public class ImmunizationHistoryDto
    {
        public int Id { get; set; }
        public int MedicalHistoryId { get; set; }

        [Required(ErrorMessage = "Flu vaccination status is required")]
        public bool HasFlu { get; set; }

        [RequiredIf(nameof(HasFlu), true, ErrorMessage = "Flu vaccination date is required")]
        public DateTime? FluDate { get; set; }

        [Required(ErrorMessage = "Tetanus vaccination status is required")]
        public bool HasTetanus { get; set; }

        [RequiredIf(nameof(HasTetanus), true, ErrorMessage = "Tetanus vaccination date is required")]
        public DateTime? TetanusDate { get; set; }

        [Required(ErrorMessage = "Pneumonia vaccination status is required")]
        public bool HasPneumonia { get; set; }

        [RequiredIf(nameof(HasPneumonia), true, ErrorMessage = "Pneumonia vaccination date is required")]
        public DateTime? PneumoniaDate { get; set; }

        [Required(ErrorMessage = "Hepatitis A vaccination status is required")]
        public bool HasHepA { get; set; }

        [RequiredIf(nameof(HasHepA), true, ErrorMessage = "Hepatitis A vaccination date is required")]
        public DateTime? HepADate { get; set; }

        [Required(ErrorMessage = "Hepatitis B vaccination status is required")]
        public bool HasHepB { get; set; }

        [RequiredIf(nameof(HasHepB), true, ErrorMessage = "Hepatitis B vaccination date is required")]
        public DateTime? HepBDate { get; set; }
    }
    
    public class SocialHistoryDto
    {
        [StringLength(50, ErrorMessage = "Exercise type cannot exceed 50 characters")]
        public string ExerciseType { get; set; }

        [StringLength(50, ErrorMessage = "Exercise frequency cannot exceed 50 characters")]
        public string ExerciseFrequency { get; set; }

        [Range(0, 10, ErrorMessage = "Packs per day must be between 0 and 10")]
        public int? PacksPerDay { get; set; }

        [Range(0, 100, ErrorMessage = "Years smoked must be between 0 and 100")]
        public int? YearsSmoked { get; set; }

        [Range(1900, 2100, ErrorMessage = "Year stopped must be between 1900 and 2100")]
        public int? YearStopped { get; set; }
    }
    
    // Result of medical history operations
    public class MedicalHistoryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
        public MedicalHistoryData Data { get; set; }
        
        public static MedicalHistoryResult Successful(MedicalHistoryData data = null)
        {
            return new MedicalHistoryResult
            {
                Success = true,
                Data = data
            };
        }
        
        public static MedicalHistoryResult Failed(string message, List<string> errors = null)
        {
            return new MedicalHistoryResult
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
    
    // Data returned from medical history queries
    public class MedicalHistoryData
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Address { get; set; }
        public string BloodType { get; set; }
        public int Age { get; set; }
        public decimal Weight { get; set; }
        public decimal Height { get; set; }
        public string Gender { get; set; }
        public bool HasHighBloodPressure { get; set; }
        public bool HasLowBloodPressure { get; set; }
        public bool HasDiabetes { get; set; }
        public string DiabetesType { get; set; }
        public bool HasAllergies { get; set; }
        public string AllergiesDetails { get; set; }
        public bool HasSurgeryHistory { get; set; }
        public string BirthControlMethod { get; set; }
        public bool HasBloodTransfusionObjection { get; set; }
        public List<MedicalConditionDto> MedicalConditions { get; set; } = new();
        public FamilyHistoryDto FamilyHistory { get; set; }
        public List<SurgicalHistoryDto> SurgicalHistories { get; set; } = new();
        public ImmunizationHistoryDto ImmunizationHistory { get; set; }
        public SocialHistoryDto SocialHistory { get; set; }
        public string QrCode { get; set; }
        public DateTime? QrCodeGeneratedAt { get; set; }
        public DateTime? QrCodeExpiresAt { get; set; }
    }
    
    // QR Code result
    public class QrCodeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string QrCodeData { get; set; }
        
        public static QrCodeResult Successful(string qrCodeData)
        {
            return new QrCodeResult
            {
                Success = true,
                Message = "QR code generated successfully",
                QrCodeData = qrCodeData
            };
        }
        
        public static QrCodeResult Failed(string message)
        {
            return new QrCodeResult
            {
                Success = false,
                Message = message
            };
        }
    }
} 