using ILLVentApp.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ILLVentApp.Application.Services.DoctorService;

namespace ILLVentApp.Application.Interfaces
{
    public interface IDoctorService
    {
        Task<IEnumerable<DoctorListDTO>> GetAllDoctorsAsync();
        Task<DoctorDetailsDTO> GetDoctorDetailsAsync(int id);
        Task<IEnumerable<TimeSlotDTO>> GetDoctorDayScheduleAsync(int doctorId, DateTime date);
        Task<AppointmentResult> CreateAppointmentAsync(AppointmentRequestWithUser appointment);
        Task<AppointmentCancellationResponse> CancelAppointmentAsync(int appointmentId, string userId);
        Task<IEnumerable<AppointmentResponseDTO>> GetUserAppointmentsAsync(string userId);
    }
} 