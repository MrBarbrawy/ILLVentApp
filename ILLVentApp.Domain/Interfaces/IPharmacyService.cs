using ILLVentApp.Domain.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IPharmacyService
    {
        Task<List<PharmacyDto>> GetAllPharmaciesAsync();
    }
} 