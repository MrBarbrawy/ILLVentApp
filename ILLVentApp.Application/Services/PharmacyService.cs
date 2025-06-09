using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;

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
                    PharmacyId = p.PharmacyId,
                    Name = p.Name,
                    Description = p.Description,
                    Thumbnail = p.Thumbnail,
                    ImageUrl = p.ImageUrl,
                    Location = p.Location,
                    Rating = p.Rating,
                    ContactNumber = p.ContactNumber,
                    AcceptPrivateInsurance = p.AcceptPrivateInsurance,
                    HasContract = p.HasContract,
                    WebsiteUrl = p.WebsiteUrl
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

        public async Task<PharmacyDto> GetPharmacyByIdAsync(int id)
        {
            var pharmacy = await _context.Set<Pharmacy>()
                .Where(p => p.PharmacyId == id)
                .Select(p => new Pharmacy
                {
                    PharmacyId = p.PharmacyId,
                    Name = p.Name,
                    Description = p.Description,
                    Thumbnail = p.Thumbnail,
                    ImageUrl = p.ImageUrl,
                    Location = p.Location,
                    Rating = p.Rating,
                    ContactNumber = p.ContactNumber,
                    AcceptPrivateInsurance = p.AcceptPrivateInsurance,
                    HasContract = p.HasContract,
                    WebsiteUrl = p.WebsiteUrl
                })
                .FirstOrDefaultAsync();

            if (pharmacy != null)
            {
                AddFullUrls(pharmacy);
            }

            return _mapper.Map<PharmacyDto>(pharmacy);
        }

        public async Task<PharmacyDto> CreatePharmacyAsync(CreatePharmacyDto pharmacyDto)
        {
            var pharmacy = _mapper.Map<Pharmacy>(pharmacyDto);
            _context.Set<Pharmacy>().Add(pharmacy);
            await _context.SaveChangesAsync();

            // Return the full pharmacy with URLs
            var createdPharmacy = await GetPharmacyByIdAsync(pharmacy.PharmacyId);
            return createdPharmacy;
        }

        public async Task<PharmacyDto> UpdatePharmacyAsync(UpdatePharmacyDto pharmacyDto)
        {
            var existingPharmacy = await _context.Set<Pharmacy>()
                .FirstOrDefaultAsync(p => p.PharmacyId == pharmacyDto.PharmacyId);

            if (existingPharmacy == null)
            {
                throw new InvalidOperationException($"Pharmacy with ID {pharmacyDto.PharmacyId} not found.");
            }

            // Update the existing pharmacy with new values
            _mapper.Map(pharmacyDto, existingPharmacy);
            
            _context.Set<Pharmacy>().Update(existingPharmacy);
            await _context.SaveChangesAsync();

            // Return the updated pharmacy with URLs
            var updatedPharmacy = await GetPharmacyByIdAsync(existingPharmacy.PharmacyId);
            return updatedPharmacy;
        }

        public async Task<bool> DeletePharmacyAsync(int pharmacyId)
        {
            var pharmacy = await _context.Set<Pharmacy>().FindAsync(pharmacyId);
            if (pharmacy == null)
                return false;

            _context.Set<Pharmacy>().Remove(pharmacy);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 