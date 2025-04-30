using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ILLVentApp.Domain.Models
{
    public class TimeSlot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TimeSlotId { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public bool IsAvailable { get; set; } = true;

        // Navigation properties
        [ForeignKey("ScheduleId")]
        public virtual Schedule Schedule { get; set; }

        public virtual Appointment Appointment { get; set; }
    }
} 