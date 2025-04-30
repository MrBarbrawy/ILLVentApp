using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ILLVentApp.Domain.Models
{
    public class Appointment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AppointmentId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string PatientName { get; set; }

        [Required]
        [Range(0, 120)]
        public int PatientAge { get; set; }

        [Required]
        [MaxLength(10)]
        public string PatientGender { get; set; }

        [Required]
        [MaxLength(20)]
        [Phone]
        public string PatientPhoneNumber { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Confirmed";  // Confirmed, Cancelled, Completed

        // Navigation properties
        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; }
    }
} 