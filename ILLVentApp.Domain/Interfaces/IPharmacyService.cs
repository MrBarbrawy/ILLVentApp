using ILLVentApp.Domain.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IPharmacyService
    {
        Task<List<PharmacyDto>> GetAllPharmaciesAsync();
        Task<PharmacyDto> CreatePharmacyAsync(CreatePharmacyDto pharmacyDto);
        Task<bool> DeletePharmacyAsync(int pharmacyId);
    }
} 