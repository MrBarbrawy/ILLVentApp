using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ILLVentApp.Application.DTOs;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ILLVentApp.Application.Services
{
    public class HospitalService : IHospitalService
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string AzureBaseUrl = "https://illventapp.azurewebsites.net";

        public HospitalService(IAppDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<HospitalDto>> GetAllHospitalsAsync()
        {
            var hospitals = await _context.Set<Hospital>()
			   .Select(h => new Hospital
			   {
				   Name = h.Name,
				   Description = h.Description,
				   Thumbnail = h.Thumbnail,
				   ImageUrl = h.ImageUrl,
				   Location = h.Location,
				   Rating = h.Rating,
				   ContactNumber = h.ContactNumber,
				   Established = h.Established,
				   Specialties = h.Specialties
			   })
                .ToListAsync();

		   // Add full URLs to images
		   hospitals = hospitals.Select(h => AddFullUrls(h)).ToList();

            return _mapper.Map<List<HospitalDto>>(hospitals);
	   }



        private Hospital AddFullUrls(Hospital hospital)
        {
            if (!string.IsNullOrEmpty(hospital.Thumbnail))
            {
                hospital.Thumbnail = $"{AzureBaseUrl}{hospital.Thumbnail}";
            }
            if (!string.IsNullOrEmpty(hospital.ImageUrl))
            {
                hospital.ImageUrl = $"{AzureBaseUrl}{hospital.ImageUrl}";
            }
            return hospital;
        }


		/*

	   public async Task<HospitalDto> GetHospitalByIdAsync(int id)
	   {
		   var hospital = await _context.Set<Hospital>()
			   .Where(h => h.HospitalId == id)
			   .Select(h => new Hospital
			   {
				   Name = h.Name,
				   Description = h.Description,
				   Thumbnail = h.Thumbnail,
				   ImageUrl = h.ImageUrl,
				   Location = h.Location,
				   Rating = h.Rating,
				   ContactNumber = h.ContactNumber,
				   Established = h.Established,
				   Specialties = h.Specialties
			   })
			   .FirstOrDefaultAsync();

		   if (hospital != null)
		   {
			   AddFullUrls(hospital);
		   }

		   return _mapper.Map<HospitalDto>(hospital);
	   }

	   public async Task<List<HospitalDto>> GetHospitalsByLocationAsync(double latitude, double longitude, double radiusInKm)
	   {
		   var hospitals = await _context.Set<Hospital>()
			   .Where(h => h.IsAvailable)
			   .Select(h => new Hospital
			   {
				   Name = h.Name,
				   Description = h.Description,
				   Thumbnail = h.Thumbnail,
				   ImageUrl = h.ImageUrl,
				   Location = h.Location,
				   Rating = h.Rating,
				   ContactNumber = h.ContactNumber,
				   Established = h.Established,
				   Specialties = h.Specialties,
				   Latitude = h.Latitude,
				   Longitude = h.Longitude
			   })
			   .ToListAsync();

		   // Filter hospitals within the specified radius
		   var nearbyHospitals = hospitals.Where(h =>
		   {
			   var distance = CalculateDistance(latitude, longitude, h.Latitude, h.Longitude);
			   return distance <= radiusInKm;
		   }).ToList();

		   // Add full URLs to images
		   nearbyHospitals = nearbyHospitals.Select(h => AddFullUrls(h)).ToList();

		   return _mapper.Map<List<HospitalDto>>(nearbyHospitals);
	   }

	   public async Task<HospitalDto> GetNearestAvailableHospitalAsync(double latitude, double longitude)
	   {
		   var hospitals = await _context.Set<Hospital>()
			   .Where(h => h.IsAvailable && h.HasContract)
			   .Select(h => new Hospital
			   {
				   Name = h.Name,
				   Description = h.Description,
				   Thumbnail = h.Thumbnail,
				   ImageUrl = h.ImageUrl,
				   Location = h.Location,
				   Rating = h.Rating,
				   ContactNumber = h.ContactNumber,
				   Established = h.Established,
				   Specialties = h.Specialties,
				   Latitude = h.Latitude,
				   Longitude = h.Longitude
			   })
			   .ToListAsync();

		   if (!hospitals.Any())
			   return null;

		   // Find the nearest hospital
		   var nearestHospital = hospitals
			   .OrderBy(h => CalculateDistance(latitude, longitude, h.Latitude, h.Longitude))
			   .FirstOrDefault();

		   if (nearestHospital != null)
		   {
			   AddFullUrls(nearestHospital);
		   }

		   return _mapper.Map<HospitalDto>(nearestHospital);
	   }

	   public async Task<bool> IsHospitalAvailableForEmergencyAsync(int hospitalId)
	   {
		   var hospital = await _context.Set<Hospital>()
			   .Include(h => h.Ambulances)
			   .FirstOrDefaultAsync(h => h.HospitalId == hospitalId);

		   return hospital != null && 
				  hospital.IsAvailable && 
				  hospital.HasContract;
	   }

	   private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
	   {
		   // Haversine formula to calculate distance between two points on Earth
		   const double R = 6371; // Earth's radius in kilometers
		   var dLat = ToRadians(lat2 - lat1);
		   var dLon = ToRadians(lon2 - lon1);
		   var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
				   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
				   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		   var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		   return R * c;
	   }

	   private double ToRadians(double angle)
	   {
		   return Math.PI * angle / 180.0;
	   }


	   */
	}
}