using AutoMapper;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Models;
using System.Collections.Generic;

namespace ILLVentApp.Application.Mappings
{
    public class HospitalProfile : Profile
    {
        public HospitalProfile()
        {
            CreateMap<Hospital, HospitalDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Thumbnail, opt => opt.MapFrom(src => src.Thumbnail))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.ContactNumber, opt => opt.MapFrom(src => src.ContactNumber))
                .ForMember(dest => dest.Established, opt => opt.MapFrom(src => src.Established))
                .ForMember(dest => dest.Specialties, opt => opt.MapFrom(src => src.Specialties));
        }
    }
}