using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ILLVentApp.Domain.DTOs
{
    public class DoctorDetailsDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string ImageUrl { get; set; }
        public double Rating { get; set; }
        public List<DayAvailabilityDTO> AvailableDays { get; set; }
        public List<AppointmentDTO> Appointments { get; set; }
    }

    public class DayAvailabilityDTO
    {
        public DateTime Date { get; set; }
        public bool IsAvailable { get; set; }
        public string FormattedDate { get; set; }
    }

    public class TimeSlotDTO
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsReserved { get; set; }
        public string FormattedStartTime { get; set; }
        public string FormattedEndTime { get; set; }
    }

    public class AvailableDayDTO
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; }
        public bool HasAvailableSlots { get; set; }
    }

    public class AppointmentDTO
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string UserId { get; set; }
        public string PatientName { get; set; }
        public int PatientAge { get; set; }
        public string PatientGender { get; set; }
        public string PatientPhoneNumber { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AppointmentRequestWithUser : AppointmentRequestDTO
    {
        public string UserId { get; set; }
    }

    public class AppointmentRequestDTO
    {
        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Start time must be in HH:mm format")]
        public string StartTime { get; set; }

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
    }

    public class AppointmentResponseDTO
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialty { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string DayOfWeek { get; set; }
        public string FormattedTime { get; set; }
        public string PatientName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DoctorListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string Education { get; set; }
        public string Location { get; set; }
        public double Rating { get; set; }
        public bool AcceptInsurance { get; set; }
        public string ImageUrl { get; set; }
        public string Thumbnail { get; set; }
        public string Hospital { get; set; }
    }
} 