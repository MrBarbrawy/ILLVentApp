using Microsoft.EntityFrameworkCore;
using ILLVentApp.Domain.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; set; }
        DbSet<EmergencyRescueRequest> EmergencyRescueRequests { get; set; }
        DbSet<Hospital> Hospitals { get; set; }
        DbSet<Ambulance> Ambulances { get; set; }
        DbSet<Driver> Drivers { get; set; }
        DbSet<Pharmacy> Pharmacies { get; set; }
        DbSet<Doctor> Doctors { get; set; }
        DbSet<Schedule> Schedules { get; set; }
        DbSet<TimeSlot> TimeSlots { get; set; }
        DbSet<Appointment> Appointments { get; set; }
        DbSet<Deals> Deals { get; set; }
        DbSet<Payment> Payments { get; set; }
        DbSet<MedicalHistory> MedicalHistories { get; set; }
        DbSet<MedicalCondition> MedicalConditions { get; set; }
        DbSet<FamilyHistory> FamilyHistories { get; set; }
        DbSet<SurgicalHistory> SurgicalHistories { get; set; }
        DbSet<ImmunizationHistory> ImmunizationHistories { get; set; }
        DbSet<SocialHistory> SocialHistories { get; set; }
        DbSet<Product> Products { get; set; }
        DbSet<Order> Orders { get; set; }
        DbSet<OrderItem> OrderItems { get; set; }
        DbSet<CartItem> CartItems { get; set; }
        
        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        DbSet<T> Set<T>() where T : class;
        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    }
} 