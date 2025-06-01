using AutoMapper;
using ILLVentApp.Application.DTOs;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Application.Mappings
{
	public class AuthProfile : Profile
	{
		public AuthProfile()
		{

			;


			CreateMap<RegisterCommand, User>()
	        .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
	        .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email)) // Add this line
	        .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => false))
	        .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
	        .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
	        .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

			// Register Mapping
			CreateMap<RegisterRequest, RegisterCommand>()
			.ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
			.ForMember(dest => dest.Surname, opt => opt.MapFrom(src => src.Surname))
			.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
			.ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password));


			CreateMap<ResetPasswordRequest, ResetPasswordCommand>();

			// Login Mapping
			CreateMap<LoginRequest, LoginCommand>();

			// Reset Password Mapping
			

			// Confirm Email Mapping
			CreateMap<ConfirmEmailRequest, ConfirmEmailCommand>();

			// Result Mapping
			CreateMap<AuthResult, AuthResponse>()
				.ForMember(dest => dest.Token, opt => opt.MapFrom(src => src.Token))
				.ForMember(dest => dest.Success, opt => opt.MapFrom(src => src.Success))
				.ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
				.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
				.ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
				.ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName));
		}

	}



}
