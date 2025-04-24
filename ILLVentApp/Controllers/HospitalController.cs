using System.Collections.Generic;
using System.Threading.Tasks;
using ILLVentApp.Application.DTOs;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ILLVentApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
	[Authorize]
	public class HospitalController : ControllerBase
    {
        private readonly IHospitalService _hospitalService;

        public HospitalController(IHospitalService hospitalService)
        {
            _hospitalService = hospitalService;
        }

        [HttpGet]
        public async Task<ActionResult<List<HospitalDto>>> GetAllHospitals()
        {
            var hospitals = await _hospitalService.GetAllHospitalsAsync();
            return Ok(hospitals);
        }


    }
}