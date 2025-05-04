using AutoMapper;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Models;

namespace ILLVentApp.Application.Mappings
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.Thumbnail, opt => opt.MapFrom(src => src.Thumbnail))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.ProductType, opt => opt.MapFrom(src => src.ProductType))
                .ForMember(dest => dest.HasNFC, opt => opt.MapFrom(src => src.HasNFC))
                .ForMember(dest => dest.HasMedicalDataStorage, opt => opt.MapFrom(src => src.HasMedicalDataStorage))
                .ForMember(dest => dest.HasRescueProtocol, opt => opt.MapFrom(src => src.HasRescueProtocol))
                .ForMember(dest => dest.HasVitalSensors, opt => opt.MapFrom(src => src.HasVitalSensors))
                .ForMember(dest => dest.TechnicalDetails, opt => opt.MapFrom(src => src.TechnicalDetails))
                .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity));

            CreateMap<ProductDto, Product>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore());
        }
    }
} 