using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ILLVentApp.Domain.Models
{
    public class Doctor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DoctorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(100)]
        public string Specialty { get; set; }

        [MaxLength(500)]
        public string Education { get; set; }

        [MaxLength(100)]
        public string Hospital { get; set; }

        [MaxLength(255)]
        public string ImageUrl { get; set; }

        [MaxLength(255)]
        public string Thumbnail { get; set; }

        [Range(0, 5)]
        public double Rating { get; set; }

        [MaxLength(500)]
        public string Location { get; set; }

        public bool AcceptInsurance { get; set; }

        // Working hours
        [Required]
        public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0); // 9 AM default

        [Required]
        public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0); // 5 PM default

        [Required]
        [Range(15, 120)]
        public int SlotDurationMinutes { get; set; } = 30; // 30 minutes default

        [Required]
        public string WorkingDays { get; set; } = "1,2,3,4,5"; // Monday to Friday by default

        [NotMapped]
        public DayOfWeek[] WorkingDaysArray
        {
            get
            {
                if (string.IsNullOrEmpty(WorkingDays)) return Array.Empty<DayOfWeek>();
                return WorkingDays.Split(',')
                    .Select(d => (DayOfWeek)int.Parse(d))
                    .ToArray();
            }
            set
            {
                WorkingDays = string.Join(",", value.Select(d => (int)d));
            }
        }
        
        // Navigation properties
        public virtual ICollection<Schedule> Schedules { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; }

        public Doctor()
        {
            Schedules = new HashSet<Schedule>();
            Appointments = new HashSet<Appointment>();
        }
    }
} 