using ILLVentApp.Application.Interfaces;
using ILLVentApp.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Globalization;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using ILLVentApp.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using ILLVentApp.Domain.Interfaces;

namespace ILLVentApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
	public class DoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public DoctorController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DoctorDetailsDTO>>> GetDoctors()
        {
            var doctors = await _doctorService.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorDetailsDTO>> GetDoctor(int id)
        {
            var doctor = await _doctorService.GetDoctorDetailsAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            return Ok(doctor);
        }

        [HttpGet("{doctorId}/schedule")]
        public async Task<ActionResult<IEnumerable<TimeSlotDTO>>> GetDoctorDaySchedule(int doctorId, [FromQuery] string date)
        {
            try
            {
                if (!DateTime.TryParse(date, out DateTime requestedDate))
                {
                    return BadRequest("Invalid date format. Please use YYYY-MM-DD format (e.g. 2025-05-02)");
                }

                var schedule = await _doctorService.GetDoctorDayScheduleAsync(doctorId, requestedDate);
                if (schedule == null)
                {
                    return NotFound();
                }
                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

    
        [HttpPost("appointment")]
        public async Task<ActionResult<AppointmentResponseDTO>> CreateAppointment(AppointmentRequestDTO appointment)
        {
            try
            {
                // Get the user ID from the authenticated user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Create a new appointment request with the user ID
                var appointmentWithUser = new AppointmentRequestWithUser
                {
                    DoctorId = appointment.DoctorId,
                    AppointmentDate = appointment.AppointmentDate,
                    StartTime = appointment.StartTime,
                    PatientName = appointment.PatientName,
                    PatientAge = appointment.PatientAge,
                    PatientGender = appointment.PatientGender,
                    PatientPhoneNumber = appointment.PatientPhoneNumber,
                    UserId = userId
                };

                var result = await _doctorService.CreateAppointmentAsync(appointmentWithUser);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        
        [HttpPost("appointment/{id}/cancel")]
        public async Task<ActionResult> CancelAppointment(int id)
        {
            try
            {
                // Get the user ID from the authenticated user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var result = await _doctorService.CancelAppointmentAsync(id, userId);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

  
        [HttpGet("appointments")]
        public async Task<ActionResult<IEnumerable<AppointmentResponseDTO>>> GetUserAppointments()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var appointments = await _doctorService.GetUserAppointmentsAsync(userId);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 