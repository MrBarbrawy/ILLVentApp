using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using ILLVentApp.Domain.DTOs;

namespace ILLVentApp.Domain.Models
{
	public class MedicalHistory
	{
		public int Id { get; set; }
		public string UserId { get; set; }

		// Basic Info (Tab 1)
		[Required(ErrorMessage = "Address is required")]
		[StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
		public string Address { get; set; }

		[Required(ErrorMessage = "Blood type is required")]
		[StringLength(3, ErrorMessage = "Blood type must be 3 characters (e.g., A+, B-)")]
		[RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Invalid blood type format")]
		public string BloodType { get; set; }

		[Required(ErrorMessage = "Age is required")]
		[Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
		public int Age { get; set; }

		[Required(ErrorMessage = "Weight is required")]
		[Range(0.1, 500, ErrorMessage = "Weight must be between 0.1 and 500 kg")]
		public decimal Weight { get; set; }

		[Required(ErrorMessage = "Height is required")]
		[Range(0.1, 300, ErrorMessage = "Height must be between 0.1 and 300 cm")]
		public decimal Height { get; set; }

		[Required(ErrorMessage = "Gender is required")]
		[StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters")]
		public string Gender { get; set; }

		[Required(ErrorMessage = "Please specify if you have high blood pressure")]
		public bool HasHighBloodPressure { get; set; }

		[Required(ErrorMessage = "Please specify if you have low blood pressure")]
		public bool HasLowBloodPressure { get; set; }

		[Required(ErrorMessage = "Please specify if you have diabetes")]
		public bool HasDiabetes { get; set; }

		[StringLength(20, ErrorMessage = "Diabetes type cannot exceed 20 characters")]
		public string DiabetesType { get; set; }

		// Medical Conditions
		public List<MedicalCondition> MedicalConditions { get; set; } = new List<MedicalCondition>();

		// Allergies
		[Required(ErrorMessage = "Please specify if you have any allergies")]
		public bool HasAllergies { get; set; }

		[StringLength(500, ErrorMessage = "Allergies details cannot exceed 500 characters")]
		public string AllergiesDetails { get; set; }

		// Family History
		public List<FamilyHistory> FamilyHistory { get; set; } = new List<FamilyHistory>();

		// Surgical History
		[Required(ErrorMessage = "Please specify if you have any surgical history")]
		public bool HasSurgeryHistory { get; set; }

		public List<SurgicalHistory> SurgicalHistories { get; set; } = new List<SurgicalHistory>();

		// Birth Control & Blood Transfusion
		[StringLength(100, ErrorMessage = "Birth control method cannot exceed 100 characters")]
		public string BirthControlMethod { get; set; }

		[Required(ErrorMessage = "Please specify if you have any blood transfusion objections")]
		public bool HasBloodTransfusionObjection { get; set; }

		// Immunization
		public ImmunizationHistory ImmunizationHistory { get; set; }

		// Social History
		public SocialHistory SocialHistory { get; set; }

		// QR Code
		[StringLength(255, ErrorMessage = "QR code cannot exceed 255 characters")]
		public string QrCode { get; set; }

		public DateTime QrCodeGeneratedAt { get; set; }
		public DateTime QrCodeExpiresAt { get; set; }

		// Navigation property
		public User User { get; set; }
	}

	// Supporting Models
	public class MedicalCondition
	{
		public int Id { get; set; }
		public int MedicalHistoryId { get; set; }

		[Required(ErrorMessage = "Condition name is required")]
		[StringLength(100, ErrorMessage = "Condition name cannot exceed 100 characters")]
		public string Condition { get; set; }

		[StringLength(500, ErrorMessage = "Condition details cannot exceed 500 characters")]
		public string Details { get; set; }
	}

	public class FamilyHistory
	{
		public int Id { get; set; }
		public int MedicalHistoryId { get; set; }
		public MedicalHistory MedicalHistory { get; set; }

		// Cancer and Polyps
		public bool HasCancerPolyps { get; set; }
		[RequiredIf(nameof(HasCancerPolyps), true, ErrorMessage = "Relationship is required when Cancer/Polyp is selected")]
		public string CancerPolypsRelationship { get; set; }

		// Anemia
		public bool HasAnemia { get; set; }
		[RequiredIf(nameof(HasAnemia), true, ErrorMessage = "Relationship is required when Anemia is selected")]
		public string AnemiaRelationship { get; set; }

		// Diabetes
		public bool HasDiabetes { get; set; }
		[RequiredIf(nameof(HasDiabetes), true, ErrorMessage = "Relationship is required when Diabetes is selected")]
		public string DiabetesRelationship { get; set; }

		// Blood Clots
		public bool HasBloodClots { get; set; }
		[RequiredIf(nameof(HasBloodClots), true, ErrorMessage = "Relationship is required when Blood Clots is selected")]
		public string BloodClotsRelationship { get; set; }

		// Heart Disease
		public bool HasHeartDisease { get; set; }
		[RequiredIf(nameof(HasHeartDisease), true, ErrorMessage = "Relationship is required when Heart Disease is selected")]
		public string HeartDiseaseRelationship { get; set; }

		// Stroke
		public bool HasStroke { get; set; }
		[RequiredIf(nameof(HasStroke), true, ErrorMessage = "Relationship is required when Stroke is selected")]
		public string StrokeRelationship { get; set; }

		// High Blood Pressure
		public bool HasHighBloodPressure { get; set; }
		[RequiredIf(nameof(HasHighBloodPressure), true, ErrorMessage = "Relationship is required when High Blood Pressure is selected")]
		public string HighBloodPressureRelationship { get; set; }

		// Anesthesia Reaction
		public bool HasAnesthesiaReaction { get; set; }
		[RequiredIf(nameof(HasAnesthesiaReaction), true, ErrorMessage = "Relationship is required when Anesthesia Reaction is selected")]
		public string AnesthesiaReactionRelationship { get; set; }

		// Bleeding Problems
		public bool HasBleedingProblems { get; set; }
		[RequiredIf(nameof(HasBleedingProblems), true, ErrorMessage = "Relationship is required when Bleeding Problems is selected")]
		public string BleedingProblemsRelationship { get; set; }

		// Hepatitis
		public bool HasHepatitis { get; set; }
		[RequiredIf(nameof(HasHepatitis), true, ErrorMessage = "Relationship is required when Hepatitis is selected")]
		public string HepatitisRelationship { get; set; }

		// Other Conditions
		public bool HasOtherCondition { get; set; }
		[RequiredIf(nameof(HasOtherCondition), true, ErrorMessage = "Details are required when Other Condition is selected")]
		public string OtherConditionDetails { get; set; }
		[RequiredIf(nameof(HasOtherCondition), true, ErrorMessage = "Relationship is required when Other Condition is selected")]
		public string OtherConditionRelationship { get; set; }
	}

	public class SurgicalHistory
	{
		public int Id { get; set; }
		public int MedicalHistoryId { get; set; }

		[Required(ErrorMessage = "Surgery type is required")]
		[StringLength(100, ErrorMessage = "Surgery type cannot exceed 100 characters")]
		public string SurgeryType { get; set; }

		[Required(ErrorMessage = "Surgery date is required")]
		public DateTime Date { get; set; }

		[StringLength(500, ErrorMessage = "Surgery details cannot exceed 500 characters")]
		public string Details { get; set; }
	}

	public class ImmunizationHistory
	{
		public int Id { get; set; }
		public int MedicalHistoryId { get; set; }

		[Required(ErrorMessage = "Please specify if you have had a flu shot")]
		public bool HasFlu { get; set; }

		public DateTime? FluDate { get; set; }

		[Required(ErrorMessage = "Please specify if you have had a tetanus shot")]
		public bool HasTetanus { get; set; }

		public DateTime? TetanusDate { get; set; }

		[Required(ErrorMessage = "Please specify if you have had a pneumonia shot")]
		public bool HasPneumonia { get; set; }

		public DateTime? PneumoniaDate { get; set; }

		[Required(ErrorMessage = "Please specify if you have had a hepatitis A shot")]
		public bool HasHepA { get; set; }

		public DateTime? HepADate { get; set; }

		[Required(ErrorMessage = "Please specify if you have had a hepatitis B shot")]
		public bool HasHepB { get; set; }

		public DateTime? HepBDate { get; set; }
	}

	public class SocialHistory
	{
		public int Id { get; set; }
		public int MedicalHistoryId { get; set; }

		[StringLength(50, ErrorMessage = "Exercise type cannot exceed 50 characters")]
		public string ExerciseType { get; set; }

		[StringLength(50, ErrorMessage = "Exercise frequency cannot exceed 50 characters")]
		public string ExerciseFrequency { get; set; }

		[Range(0, 100, ErrorMessage = "Packs per day must be between 0 and 100")]
		public int? PacksPerDay { get; set; }

		[Range(0, 100, ErrorMessage = "Years smoked must be between 0 and 100")]
		public int? YearsSmoked { get; set; }

		[Range(1900, 2100, ErrorMessage = "Year stopped must be between 1900 and 2100")]
		public int? YearStopped { get; set; }
	}
}
