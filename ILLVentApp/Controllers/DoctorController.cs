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
        public async Task<ActionResult<AppointmentResult>> CreateAppointment(AppointmentRequestDTO appointment)
        {
            try
            {
                // 2. Authentication/Authorization Errors - Enhanced validation
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new AppointmentResult 
                    { 
                        Success = false, 
                        Message = "Please login to book an appointment",
                        Data = null,
                        Errors = new List<string> { "User not authenticated" }
                    });
                }

                // Additional model validation
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(new AppointmentResult
                    {
                        Success = false,
                        Message = "Please check your appointment details",
                        Data = null,
                        Errors = errors
                    });
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
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                // 6. System Errors - Catch any unhandled exceptions
                return StatusCode(500, new AppointmentResult
                {
                    Success = false,
                    Message = "An unexpected error occurred. Please try again later",
                    Data = null,
                    Errors = new List<string> { "Internal server error" }
                });
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