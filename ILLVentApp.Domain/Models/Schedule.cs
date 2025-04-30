using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ILLVentApp.Domain.Models
{
    public class Schedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ScheduleId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public bool IsAvailable { get; set; } = true;

        // Navigation properties
        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; }

        public virtual ICollection<TimeSlot> TimeSlots { get; set; }

        public Schedule()
        {
            TimeSlots = new HashSet<TimeSlot>();
        }
    }
} 