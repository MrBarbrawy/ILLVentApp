using ILLVentApp.Domain.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IHospitalService
    {
        Task<List<HospitalDto>> GetAllHospitalsAsync();
        Task<HospitalDto> GetHospitalByIdAsync(int id);
        Task<List<HospitalDto>> GetHospitalsByLocationAsync(double latitude, double longitude, double radiusInKm);
        Task<HospitalDto> GetNearestAvailableHospitalAsync(double latitude, double longitude);
        Task<bool> IsHospitalAvailableForEmergencyAsync(int hospitalId);
        Task<HospitalDto> CreateHospitalAsync(CreateHospitalDto hospitalDto);
        Task<HospitalDto> UpdateHospitalAsync(UpdateHospitalDto hospitalDto);
        Task<bool> DeleteHospitalAsync(int hospitalId);
    }
}