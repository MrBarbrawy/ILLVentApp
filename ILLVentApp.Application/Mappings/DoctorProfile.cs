using AutoMapper;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Models;

namespace ILLVentApp.Application.Mappings
{
    public class DoctorProfile : Profile
    {
        public DoctorProfile()
        {
            // Mapping for list view (GetAllDoctorsAsync)
            CreateMap<Doctor, DoctorListDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Specialty, opt => opt.MapFrom(src => src.Specialty))
                .ForMember(dest => dest.Education, opt => opt.MapFrom(src => src.Education))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.AcceptInsurance, opt => opt.MapFrom(src => src.AcceptInsurance))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.Thumbnail, opt => opt.MapFrom(src => src.Thumbnail))
                .ForMember(dest => dest.Hospital, opt => opt.MapFrom(src => src.Hospital));

            // Mapping for details view (GetDoctorDetailsAsync)
            CreateMap<Doctor, DoctorDetailsDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Specialty, opt => opt.MapFrom(src => src.Specialty))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.AvailableDays, opt => opt.Ignore())
                .ForMember(dest => dest.Appointments, opt => opt.Ignore());

            // Appointment mappings
            CreateMap<AppointmentDTO, Appointment>()
                .ForMember(dest => dest.AppointmentId, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.PatientAge, opt => opt.MapFrom(src => src.PatientAge))
                .ForMember(dest => dest.PatientGender, opt => opt.MapFrom(src => src.PatientGender))
                .ForMember(dest => dest.PatientPhoneNumber, opt => opt.MapFrom(src => src.PatientPhoneNumber))
                .ForMember(dest => dest.AppointmentDate, opt => opt.MapFrom(src => src.AppointmentDate))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Doctor, opt => opt.Ignore());

            CreateMap<Appointment, AppointmentDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.AppointmentId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.PatientAge, opt => opt.MapFrom(src => src.PatientAge))
                .ForMember(dest => dest.PatientGender, opt => opt.MapFrom(src => src.PatientGender))
                .ForMember(dest => dest.PatientPhoneNumber, opt => opt.MapFrom(src => src.PatientPhoneNumber))
                .ForMember(dest => dest.AppointmentDate, opt => opt.MapFrom(src => src.AppointmentDate))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));
        }
    }
} 