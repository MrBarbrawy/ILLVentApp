using System;
using System.Collections.Generic;
using System.Linq;

namespace ILLVentApp.Domain.DTOs
{
    public class AppointmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public AppointmentResponseDTO Data { get; set; }
        public List<string> Errors { get; set; }

        public static AppointmentResult Successful(AppointmentResponseDTO data = null, string message = "Operation completed successfully")
        {
            return new AppointmentResult
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = new List<string>()
            };
        }

        public static AppointmentResult Failure(string message, List<string> errors = null)
        {
            return new AppointmentResult
            {
                Success = false,
                Message = message,
                Data = null,
                Errors = errors ?? new List<string>()
            };
        }

        public static AppointmentResult Failure(IEnumerable<string> errors)
        {
            return new AppointmentResult
            {
                Success = false,
                Message = string.Join(", ", errors),
                Data = null,
                Errors = errors?.ToList() ?? new List<string>()
            };
        }

        // Authentication/Authorization error helpers
        public static AppointmentResult UserNotAuthenticated()
        {
            return Failure("Please login to book an appointment");
        }

        public static AppointmentResult UserNotAuthorized()
        {
            return Failure("You are not authorized to perform this action");
        }

        // Date/Time validation error helpers
        public static AppointmentResult InvalidTimeFormat()
        {
            return Failure("Invalid time format. Please use HH:mm format (e.g., 14:30)");
        }

        public static AppointmentResult PastDate()
        {
            return Failure("Cannot book appointments for past dates");
        }

        public static AppointmentResult PastTimeSlot()
        {
            return Failure("This time slot has already passed. Please select a future time");
        }

        public static AppointmentResult NonWorkingDay()
        {
            return Failure("Selected date is not a working day for this doctor");
        }

        public static AppointmentResult OutsideWorkingHours(TimeSpan doctorStart, TimeSpan doctorEnd)
        {
            return Failure($"Selected time is outside doctor's working hours ({doctorStart:hh\\:mm} - {doctorEnd:hh\\:mm})");
        }

        // Appointment conflict error helpers
        public static AppointmentResult TimeSlotTaken()
        {
            return Failure("This time slot is no longer available. Please select a different time");
        }

        public static AppointmentResult ExistingAppointment(string doctorName, DateTime existingDate, TimeSpan existingTime)
        {
            return Failure($"You already have an appointment with Dr. {doctorName} on {existingDate:dddd, MMMM d, yyyy} at {existingTime:hh\\:mm}. Please cancel your existing appointment before booking a new one");
        }

        public static AppointmentResult DoctorNotFound()
        {
            return Failure("Selected doctor is not available or does not exist");
        }

        public static AppointmentResult DoctorNotAvailable()
        {
            return Failure("Selected doctor is currently not available for appointments");
        }

        // System error helpers
        public static AppointmentResult DatabaseError()
        {
            return Failure("A system error occurred while processing your appointment. Please try again later");
        }

        public static AppointmentResult TransactionError()
        {
            return Failure("Failed to save appointment due to system error. Please try again");
        }

        public static AppointmentResult ConcurrencyError()
        {
            return Failure("Another user may have booked this slot. Please refresh and try again");
        }

        public static AppointmentResult SystemError(string details = null)
        {
            var message = "A system error occurred. Please try again later";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" (Error: {details})";
            }
            return Failure(message);
        }
    }
} 