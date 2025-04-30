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

        public async Task<IEnumerable<TimeSlotDTO>> GetDoctorDayScheduleAsync(int doctorId, DateTime date)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

            if (doctor == null)
                throw new NotFoundException("Doctor not found");

            // Create DateTimeOffset from input date to handle timezone
            var dateOffset = new DateTimeOffset(date);
            
            // Since we're dealing with +03:00 timezone, we need to add a day to UTC
            var utcDate = dateOffset.UtcDateTime.Date.AddDays(1);
            var currentUtcDate = DateTime.UtcNow.Date;

            // Use the local date for working day check
            if (!doctor.WorkingDaysArray.Contains(date.DayOfWeek))
                return Enumerable.Empty<TimeSlotDTO>();

            if (utcDate < currentUtcDate)
                return Enumerable.Empty<TimeSlotDTO>();

            // Generate time slots
            var timeSlots = new List<TimeSlotDTO>();
            var currentTime = doctor.StartTime;

            while (currentTime.Add(TimeSpan.FromMinutes(doctor.SlotDurationMinutes)) <= doctor.EndTime)
            {
                var slotStartUtc = utcDate.Add(currentTime);
                var slotEndUtc = slotStartUtc.Add(TimeSpan.FromMinutes(doctor.SlotDurationMinutes));

                // Convert to local time for display
                var slotStartLocal = slotStartUtc.Add(dateOffset.Offset);
                var slotEndLocal = slotEndUtc.Add(dateOffset.Offset);

                timeSlots.Add(new TimeSlotDTO
                {
                    StartTime = slotStartUtc,
                    EndTime = slotEndUtc,
                    IsReserved = false,
                    FormattedStartTime = slotStartLocal.ToString("dddd, MMMM d, yyyy hh:mm tt"),
                    FormattedEndTime = slotEndLocal.ToString("dddd, MMMM d, yyyy hh:mm tt")
                });

                currentTime = currentTime.Add(TimeSpan.FromMinutes(doctor.SlotDurationMinutes));
            }

            // Get appointments
            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate.Date == utcDate.Date &&
                           a.Status != "Cancelled")
                .ToListAsync();

            // Mark booked slots
            foreach (var appointment in appointments)
            {
                foreach (var slot in timeSlots)
                {
                    if (slot.StartTime.TimeOfDay >= appointment.StartTime &&
                        slot.StartTime.TimeOfDay < appointment.EndTime)
                    {
                        slot.IsReserved = true;
                    }
                }
            }

            var availableTimeSlots = timeSlots.Where(slot => !slot.IsReserved).ToList();

            // If it's today, remove past time slots
            if (utcDate == currentUtcDate)
            {
                var nowUtc = DateTime.UtcNow;
                availableTimeSlots = availableTimeSlots
                    .Where(slot => slot.StartTime > nowUtc)
                    .ToList();
            }

            return availableTimeSlots;
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

            // Get the local timezone offset
            var localOffset = TimeZoneInfo.Local.GetUtcOffset(currentDateTime);

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

                    var cacheKey = GetTimeSlotCacheKey(doctorId, currentDate);
                    List<TimeSlotDTO> timeSlots;

                    lock (_cacheLock)
                    {
                        if (!_timeSlotCache.TryGetValue(cacheKey, out timeSlots))
                        {
                            timeSlots = GenerateTimeSlots(doctor, currentDate, localOffset);
                            _timeSlotCache[cacheKey] = timeSlots;
                        }
                    }

                    // Create a copy of time slots to avoid modifying cached version
                    var dayTimeSlots = timeSlots.Select(ts => new TimeSlotDTO
                    {
                        StartTime = ts.StartTime,
                        EndTime = ts.EndTime,
                        IsReserved = ts.IsReserved,
                        FormattedStartTime = ts.FormattedStartTime,
                        FormattedEndTime = ts.FormattedEndTime
                    }).ToList();

                    // For today, remove time slots that are in the past
                    if (currentDate.Date == currentDateTime.Date)
                    {
                        dayTimeSlots.RemoveAll(slot => slot.StartTime.TimeOfDay <= currentDateTime.TimeOfDay);
                    }

                    var dayAppointments = bookedAppointments
                        .Where(a => a.AppointmentDate.Date == currentDate.Date)
                        .ToList();

                    // Mark booked slots as reserved
                    foreach (var appointment in dayAppointments)
                    {
                        foreach (var slot in dayTimeSlots)
                        {
                            if (slot.StartTime >= appointment.AppointmentDate.Add(appointment.StartTime) &&
                                slot.StartTime < appointment.AppointmentDate.Add(appointment.EndTime))
                            {
                                slot.IsReserved = true;
                                
                                // Update cache
                                lock (_cacheLock)
                                {
                                    var cachedSlot = _timeSlotCache[cacheKey].First(s => 
                                        s.StartTime == slot.StartTime && s.EndTime == slot.EndTime);
                                    cachedSlot.IsReserved = true;
                                }
                            }
                        }
                    }

                    // Check if there are any available slots
                    var hasAvailableSlots = dayTimeSlots.Any(s => !s.IsReserved);

                    if (hasAvailableSlots)
                    {
                        availableDays.Add(new DayAvailabilityDTO
                        {
                            Date = currentDate,
                            IsAvailable = true,
                            FormattedDate = currentDate.ToString("dddd d MMMM")  // "Monday 12 April"
                        });
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            // Clean up old cache entries
            CleanupCache();

            return availableDays;
        }

        private void CleanupCache()
        {
            lock (_cacheLock)
            {
                var today = DateTime.Today;
                var keysToRemove = _timeSlotCache.Keys
                    .Where(key => DateTime.ParseExact(key.Split('_')[1], "yyyyMMdd", null) < today)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _timeSlotCache.Remove(key);
                }
            }
        }

        private List<TimeSlotDTO> GenerateTimeSlots(Doctor doctor, DateTime date, TimeSpan offset)
        {
            var slots = new List<TimeSlotDTO>();
            var currentTime = doctor.StartTime;

            while (currentTime.Add(TimeSpan.FromMinutes(doctor.SlotDurationMinutes)) <= doctor.EndTime)
            {
                // Create DateTimeOffset to properly handle the timezone
                var startDateTimeOffset = new DateTimeOffset(
                    date.Year, date.Month, date.Day,
                    currentTime.Hours, currentTime.Minutes, 0,
                    offset);

                var endDateTimeOffset = startDateTimeOffset.AddMinutes(doctor.SlotDurationMinutes);
                
                slots.Add(new TimeSlotDTO
                {
                    StartTime = startDateTimeOffset.DateTime,
                    EndTime = endDateTimeOffset.DateTime,
                    IsReserved = false,
                    FormattedStartTime = FormatDateTime(startDateTimeOffset.DateTime),
                    FormattedEndTime = FormatDateTime(endDateTimeOffset.DateTime)
                });

                currentTime = currentTime.Add(TimeSpan.FromMinutes(doctor.SlotDurationMinutes));
            }

            return slots;
        }

        private string FormatTime(TimeSpan time)
        {
            return DateTime.Today.Add(time).ToString("hh:mm tt"); // Returns time in "09:00 AM" format
        }

        private string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("dddd, MMMM d, yyyy hh:mm tt");
        }

        private string GetFormattedWorkingDays(Doctor doctor)
        {
            var daysOfWeek = doctor.WorkingDaysArray
                .Select(d => d.ToString("G"))  // Gets the full name (Monday, Tuesday, etc.)
                .ToList();

            return string.Join(", ", daysOfWeek);
        }

        public async Task<AppointmentResponseDTO> CreateAppointmentAsync(AppointmentRequestWithUser appointmentRequest)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == appointmentRequest.DoctorId);

            if (doctor == null)
                throw new InvalidOperationException("Doctor not found.");

            // Parse the time string
            if (!TimeSpan.TryParse(appointmentRequest.StartTime, out TimeSpan startTime))
            {
                throw new InvalidOperationException("Invalid time format. Please use HH:mm format.");
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

            if (startTime < doctor.StartTime ||
                startTime.Add(TimeSpan.FromMinutes(doctor.SlotDurationMinutes)) > doctor.EndTime)
                throw new InvalidOperationException("The selected time is outside the doctor's working hours.");

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