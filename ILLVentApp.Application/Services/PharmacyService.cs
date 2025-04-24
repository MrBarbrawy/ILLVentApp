using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ILLVentApp.Application.Services
{
    public class PharmacyService : IPharmacyService
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string AzureBaseUrl = "https://illventapp.azurewebsites.net";

        public PharmacyService(IAppDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<PharmacyDto>> GetAllPharmaciesAsync()
        {
            var pharmacies = await _context.Set<Pharmacy>()
                .Select(p => new Pharmacy
                {
                    Name = p.Name,
                    Description = p.Description,
                    Thumbnail = p.Thumbnail,
                    ImageUrl = p.ImageUrl,
                    Location = p.Location,
                    Rating = p.Rating,
                    ContactNumber = p.ContactNumber,
                    AcceptPrivateInsurance = p.AcceptPrivateInsurance
                })
                .ToListAsync();

            // Add full URLs to images
            pharmacies = pharmacies.Select(p => AddFullUrls(p)).ToList();

            return _mapper.Map<List<PharmacyDto>>(pharmacies);
        }

        private Pharmacy AddFullUrls(Pharmacy pharmacy)
        {
            if (!string.IsNullOrEmpty(pharmacy.Thumbnail))
            {
                pharmacy.Thumbnail = $"{AzureBaseUrl}{pharmacy.Thumbnail}";
            }
            if (!string.IsNullOrEmpty(pharmacy.ImageUrl))
            {
                pharmacy.ImageUrl = $"{AzureBaseUrl}{pharmacy.ImageUrl}";
            }
            return pharmacy;
        }
    }
} 