using AutoMapper;
using ILLVentApp.Application.Interfaces;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ILLVentApp.Domain.Models;
using SendGrid.Helpers.Errors.Model;

namespace ILLVentApp.Application.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private const string AzureBaseUrl = "https://illventapp.azurewebsites.net";
        private static readonly Dictionary<string, List<TimeSlotDTO>> _timeSlotCache = new();
        private static readonly object _cacheLock = new();

        public DoctorService(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private void AddFullUrls(DoctorListDTO doctor)
        {
            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                doctor.ImageUrl = $"{AzureBaseUrl}{doctor.ImageUrl}";
            }
            if (!string.IsNullOrEmpty(doctor.Thumbnail))
            {
                doctor.Thumbnail = $"{AzureBaseUrl}{doctor.Thumbnail}";
            }
        }

        private void AddFullUrls(DoctorDetailsDTO doctor)
        {
            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                doctor.ImageUrl = $"{AzureBaseUrl}{doctor.ImageUrl}";
            }
        }

        private string GetTimeSlotCacheKey(int doctorId, DateTime date)
        {
            return $"{doctorId}_{date:yyyyMMdd}";
        }

        public async Task<IEnumerable<DoctorListDTO>> GetAllDoctorsAsync()
        {
            var doctors = await _context.Doctors.ToListAsync();

            var doctorDTOs = doctors.Select(doctor =>
            {
                var dto = _mapper.Map<DoctorListDTO>(doctor);
                AddFullUrls(dto);
                return dto;
            }).ToList();

            return doctorDTOs;
        }

        public async Task<DoctorDetailsDTO> GetDoctorDetailsAsync(int id)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == id);

            if (doctor == null)
                return null;

            var dto = _mapper.Map<DoctorDetailsDTO>(doctor);
            AddFullUrls(dto);
            dto.AvailableDays = await GetAvailableDaysForDoctor(id);
            return dto;
        }

        public async Task<IEnumerable<TimeSlotDTO>> GetDoctorDayScheduleAsync(int doctorId, DateTime requestedDate)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

            if (doctor == null)
                throw new NotFoundException("Doctor not found");

            // Use the date part only, ignore time
            var date = requestedDate.Date;
            var today = DateTime.Now.Date;

            // Basic validations
            if (date < today)
            {
                return Enumerable.Empty<TimeSlotDTO>();
            }

            if (!doctor.WorkingDaysArray.Contains(date.DayOfWeek))
            {
                return Enumerable.Empty<TimeSlotDTO>();
            }

            // Get booked appointments first
            var bookedAppointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate.Date == date &&
                           a.Status != "Cancelled")
                .ToListAsync();

            var timeSlots = new List<TimeSlotDTO>();
            var currentTime = doctor.StartTime; // Use doctor's actual start time
            var endTime = doctor.EndTime;       // Use doctor's actual end time
            var slotDuration = TimeSpan.FromMinutes(doctor.SlotDurationMinutes); // Use doctor's slot duration

            while (currentTime.Add(slotDuration) <= endTime)
            {
                // Skip past time slots if it's today
                if (date == today && currentTime <= DateTime.Now.TimeOfDay)
                {
                    currentTime = currentTime.Add(slotDuration);
                    continue;
                }

                // Check if this slot is booked
                var isBooked = bookedAppointments.Any(a => 
                    a.StartTime == currentTime);

                // Only add unbooked slots
                if (!isBooked)
                {
                    var slotStart = date.Add(currentTime);
                    var slotEnd = slotStart.Add(slotDuration);

                    timeSlots.Add(new TimeSlotDTO
                    {
                        StartTime = slotStart,
                        EndTime = slotEnd,
                        IsReserved = false,
                        FormattedStartTime = FormatTimeWithout24Hour(slotStart),
                        FormattedEndTime = FormatTimeWithout24Hour(slotEnd)
                    });
                }

                currentTime = currentTime.Add(slotDuration);
            }

            return timeSlots;
        }

        private async Task<List<DayAvailabilityDTO>> GetAvailableDaysForDoctor(int doctorId)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

            if (doctor == null)
                return new List<DayAvailabilityDTO>();

            var availableDays = new List<DayAvailabilityDTO>();
            var currentDateTime = DateTime.Now;
            var currentDate = currentDateTime.Date;
            var endDate = currentDate.AddDays(10); // Show next 10 days

            var workingDays = doctor.WorkingDaysArray;
            var bookedAppointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && 
                       a.AppointmentDate >= currentDate && 
                       a.AppointmentDate <= endDate && 
                       a.Status != "Cancelled")
                .ToListAsync();

            while (currentDate <= endDate)
            {
                if (workingDays.Contains(currentDate.DayOfWeek))
                {
                    // Skip if it's today and we're past working hours
                    if (currentDate.Date == currentDateTime.Date && 
                        currentDateTime.TimeOfDay > doctor.EndTime)
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    // Check for available slots
                    var dayStart = doctor.StartTime;
                    var dayEnd = doctor.EndTime;
                    var slotDuration = TimeSpan.FromMinutes(doctor.SlotDurationMinutes);
                    var hasAvailableSlots = false;

                    for (var time = dayStart; time.Add(slotDuration) <= dayEnd; time = time.Add(slotDuration))
                    {
                        // Skip past times for today
                        if (currentDate.Date == currentDateTime.Date && time <= currentDateTime.TimeOfDay)
                        {
                            continue;
                        }

                        // Check if this slot is booked
                        var isBooked = bookedAppointments.Any(a => 
                            a.AppointmentDate.Date == currentDate.Date && 
                            a.StartTime == time);

                        if (!isBooked)
                        {
                            hasAvailableSlots = true;
                            break;
                        }
                    }

                    if (hasAvailableSlots)
                    {
                        availableDays.Add(new DayAvailabilityDTO
                        {
                            Date = currentDate,
                            IsAvailable = true,
                            FormattedDate = currentDate.ToString("dddd d MMMM")
                        });
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            return availableDays;
        }

        private string FormatTime(TimeSpan time)
        {
            return DateTime.Today.Add(time).ToString("HH:mm"); // Returns time in "09:00" format (24-hour)
        }

        private string FormatTimeWithout24Hour(DateTime dateTime)
        {
            // Convert to 12-hour format without AM/PM
            var hour = dateTime.Hour;
            var minute = dateTime.Minute;
            
            if (hour == 0)
            {
                return $"12:{minute:D2}";
            }
            else if (hour <= 12)
            {
                return $"{hour}:{minute:D2}";
            }
            else
            {
                return $"{hour - 12}:{minute:D2}";
            }
        }

        private bool TryParseTimeString(string timeString, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;
            
            if (string.IsNullOrWhiteSpace(timeString))
                return false;

            // Try parsing as 12-hour format with AM/PM first
            var formats = new[] { 
                "h:mm tt", "hh:mm tt", "h:mm t", "hh:mm t",
                "h tt", "hh tt", "h t", "hh t"
            };
            
            if (DateTime.TryParseExact(timeString, formats, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out DateTime dateTime))
            {
                timeSpan = dateTime.TimeOfDay;
                return true;
            }

            // Handle special case: if it's just a number like "1:00" without AM/PM
            // For business hours context, assume 1-5 are PM, 6-12 are as-is
            if (timeString.Contains(":"))
            {
                var parts = timeString.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int hour) && int.TryParse(parts[1], out int minute))
                {
                    if (hour >= 1 && hour <= 5 && minute >= 0 && minute <= 59)
                    {
                        // Convert 1-5 to PM (13-17 in 24-hour format)
                        timeSpan = new TimeSpan(hour + 12, minute, 0);
                        return true;
                    }
                    else if (hour >= 6 && hour <= 8 && minute >= 0 && minute <= 59)
                    {
                        // Keep 6-8 as AM (morning hours)
                        timeSpan = new TimeSpan(hour, minute, 0);
                        return true;
                    }
                    else if (hour >= 9 && hour <= 12 && minute >= 0 && minute <= 59)
                    {
                        // Keep 9-12 as-is (9 AM to 12 PM)
                        timeSpan = new TimeSpan(hour, minute, 0);
                        return true;
                    }
                    else if (hour >= 13 && hour <= 23 && minute >= 0 && minute <= 59)
                    {
                        // Already in 24-hour format
                        timeSpan = new TimeSpan(hour, minute, 0);
                        return true;
                    }
                }
            }

            // Last resort: try standard TimeSpan parsing (24-hour format like "13:00")
            if (TimeSpan.TryParse(timeString, out timeSpan))
            {
                return true;
            }

            return false;
        }

        public async Task<AppointmentResponseDTO> CreateAppointmentAsync(AppointmentRequestWithUser appointmentRequest)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == appointmentRequest.DoctorId);

            if (doctor == null)
                throw new InvalidOperationException("Doctor not found.");

            // Parse the time string - handle both 12-hour and 24-hour formats
            TimeSpan startTime;
            if (!TryParseTimeString(appointmentRequest.StartTime, out startTime))
            {
                throw new InvalidOperationException("Invalid time format. Please use HH:mm format (24-hour) or h:mm AM/PM format.");
            }

            var currentDateTime = DateTime.Now;

            // Validate if trying to book a past date
            if (appointmentRequest.AppointmentDate.Date < currentDateTime.Date)
            {
                throw new InvalidOperationException("Cannot book appointments for past dates.");
            }

            // If booking for today, validate that the time slot hasn't passed
            if (appointmentRequest.AppointmentDate.Date == currentDateTime.Date && 
                startTime <= currentDateTime.TimeOfDay)
            {
                throw new InvalidOperationException("Cannot book time slots that are in the past for today.");
            }

            // Check if user already has an active appointment with this doctor
            var existingUserAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a => 
                    a.DoctorId == appointmentRequest.DoctorId &&
                    a.UserId == appointmentRequest.UserId &&
                    a.Status != "Cancelled" &&
                    a.AppointmentDate >= currentDateTime.Date);

            if (existingUserAppointment != null)
            {
                throw new InvalidOperationException(
                    $"You already have an active appointment with Dr. {doctor.Name} on " +
                    $"{existingUserAppointment.AppointmentDate.ToString("dddd, MMMM d, yyyy")} at " +
                    $"{FormatTime(existingUserAppointment.StartTime)}. " +
                    "Please cancel your existing appointment before booking a new one.");
            }

            // Validate appointment time
            if (!doctor.WorkingDaysArray.Contains(appointmentRequest.AppointmentDate.DayOfWeek))
                throw new InvalidOperationException("The selected date is not a working day for this doctor.");

            // Debug logging for working hours validation
            var endTimeForSlot = startTime.Add(TimeSpan.FromMinutes(doctor.SlotDurationMinutes));
            if (startTime < doctor.StartTime ||
                endTimeForSlot > doctor.EndTime)
            {
                throw new InvalidOperationException(
                    $"The selected time is outside the doctor's working hours. " +
                    $"Requested: {startTime:hh\\:mm} - {endTimeForSlot:hh\\:mm}, " +
                    $"Doctor hours: {doctor.StartTime:hh\\:mm} - {doctor.EndTime:hh\\:mm}");
            }

            // Check if the time slot is already booked
            var endTime = startTime.Add(TimeSpan.FromMinutes(doctor.SlotDurationMinutes));
            var existingAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a => 
                    a.DoctorId == appointmentRequest.DoctorId &&
                    a.AppointmentDate.Date == appointmentRequest.AppointmentDate.Date &&
                    a.Status != "Cancelled" &&
                    ((a.StartTime <= startTime && a.EndTime > startTime) ||
                     (a.StartTime < endTime && a.EndTime >= endTime) ||
                     (startTime <= a.StartTime && endTime >= a.EndTime)));

            if (existingAppointment != null)
            {
                throw new InvalidOperationException("This time slot is already booked. Please select a different time slot.");
            }

            // Create appointment
            var appointment = new AppointmentDTO
            {
                DoctorId = appointmentRequest.DoctorId,
                UserId = appointmentRequest.UserId,
                PatientName = appointmentRequest.PatientName,
                PatientAge = appointmentRequest.PatientAge,
                PatientGender = appointmentRequest.PatientGender,
                PatientPhoneNumber = appointmentRequest.PatientPhoneNumber,
                AppointmentDate = appointmentRequest.AppointmentDate,
                StartTime = startTime,
                EndTime = endTime,
                CreatedAt = DateTime.UtcNow,
                Status = "Confirmed"
            };

            var appointmentEntity = _mapper.Map<Appointment>(appointment);
            _context.Appointments.Add(appointmentEntity);
            await _context.SaveChangesAsync();

            // Update cache
            var cacheKey = GetTimeSlotCacheKey(doctor.DoctorId, appointmentRequest.AppointmentDate);
            lock (_cacheLock)
            {
                if (_timeSlotCache.TryGetValue(cacheKey, out var cachedSlots))
                {
                    var slot = cachedSlots.FirstOrDefault(s => 
                        s.StartTime.TimeOfDay == startTime);
                    if (slot != null)
                    {
                        slot.IsReserved = true;
                    }
                }
            }

            return new AppointmentResponseDTO
            {
                Id = appointmentEntity.AppointmentId,
                DoctorId = doctor.DoctorId,
                DoctorName = doctor.Name,
                DoctorSpecialty = doctor.Specialty,
                AppointmentDate = appointment.AppointmentDate,
                DayOfWeek = appointment.AppointmentDate.DayOfWeek.ToString(),
                FormattedTime = $"{FormatTime(appointment.StartTime)} - {FormatTime(appointment.EndTime)}",
                PatientName = appointment.PatientName,
                Status = appointment.Status,
                CreatedAt = appointment.CreatedAt
            };
        }

        public class AppointmentCancellationResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public AppointmentResponseDTO Appointment { get; set; }
        }

        public async Task<AppointmentCancellationResponse> CancelAppointmentAsync(int appointmentId, string userId)
        {
            using var transaction = await _context.BeginTransactionAsync();
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                {
                    return new AppointmentCancellationResponse
                    {
                        Success = false,
                        Message = "Appointment not found."
                    };
                }

                // Verify that the user owns this appointment
                if (appointment.UserId != userId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to cancel this appointment.");
                }

                if (appointment.Status == "Cancelled")
                {
                    return new AppointmentCancellationResponse
                    {
                        Success = false,
                        Message = "This appointment has already been cancelled."
                    };
                }

                // Store the old status for the message
                var oldStatus = appointment.Status;
                appointment.Status = "Cancelled";
                await _context.SaveChangesAsync();

                // Update cache to mark the slot as available again
                var cacheKey = GetTimeSlotCacheKey(appointment.DoctorId, appointment.AppointmentDate);
                lock (_cacheLock)
                {
                    if (_timeSlotCache.TryGetValue(cacheKey, out var cachedSlots))
                    {
                        var slot = cachedSlots.FirstOrDefault(s => 
                            s.StartTime.TimeOfDay == appointment.StartTime);
                        if (slot != null)
                        {
                            slot.IsReserved = false;
                        }
                    }
                }

                await transaction.CommitAsync();

                var appointmentResponse = new AppointmentResponseDTO
                {
                    Id = appointment.AppointmentId,
                    DoctorId = appointment.DoctorId,
                    DoctorName = appointment.Doctor.Name,
                    DoctorSpecialty = appointment.Doctor.Specialty,
                    AppointmentDate = appointment.AppointmentDate,
                    DayOfWeek = appointment.AppointmentDate.DayOfWeek.ToString(),
                    FormattedTime = $"{FormatTime(appointment.StartTime)} - {FormatTime(appointment.EndTime)}",
                    PatientName = appointment.PatientName,
                    Status = appointment.Status,
                    CreatedAt = appointment.CreatedAt
                };

                return new AppointmentCancellationResponse
                {
                    Success = true,
                    Message = $"Your appointment with Dr. {appointment.Doctor.Name} on {appointment.AppointmentDate.ToString("dddd, MMMM d, yyyy")} at {FormatTime(appointment.StartTime)} has been successfully cancelled.",
                    Appointment = appointmentResponse
                };
            }
            catch (UnauthorizedAccessException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new AppointmentCancellationResponse
                {
                    Success = false,
                    Message = "An error occurred while cancelling the appointment. Please try again later."
                };
            }
        }

        public async Task<IEnumerable<AppointmentResponseDTO>> GetUserAppointmentsAsync(string userId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.UserId == userId && a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            return appointments.Select(appointment => new AppointmentResponseDTO
            {
                Id = appointment.AppointmentId,
                DoctorId = appointment.DoctorId,
                DoctorName = appointment.Doctor.Name,
                DoctorSpecialty = appointment.Doctor.Specialty,
                AppointmentDate = appointment.AppointmentDate,
                DayOfWeek = appointment.AppointmentDate.DayOfWeek.ToString(),
                FormattedTime = $"{FormatTime(appointment.StartTime)} - {FormatTime(appointment.EndTime)}",
                PatientName = appointment.PatientName,
                Status = appointment.Status,
                CreatedAt = appointment.CreatedAt
            }).ToList();
        }
    }
} 