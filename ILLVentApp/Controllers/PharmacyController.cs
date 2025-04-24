using System.Collections.Generic;
using System.Threading.Tasks;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ILLVentApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
	[Authorize]

	public class PharmacyController : ControllerBase
    {
        private readonly IPharmacyService _pharmacyService;

        public PharmacyController(IPharmacyService pharmacyService)
        {
            _pharmacyService = pharmacyService;
        }

        [HttpGet]
        public async Task<ActionResult<List<PharmacyDto>>> GetAllPharmacies()
        {
            var pharmacies = await _pharmacyService.GetAllPharmaciesAsync();
            return Ok(pharmacies);
        }
    }
} 