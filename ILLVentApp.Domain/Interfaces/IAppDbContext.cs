using Microsoft.EntityFrameworkCore;
using ILLVentApp.Domain.Models;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<MedicalHistory> MedicalHistories { get; }
        DbSet<MedicalCondition> MedicalConditions { get; }
        DbSet<FamilyHistory> FamilyHistories { get; }
        DbSet<SurgicalHistory> SurgicalHistories { get; }
        DbSet<ImmunizationHistory> ImmunizationHistories { get; }
        DbSet<SocialHistory> SocialHistories { get; }
        DbSet<Hospital> Hospitals { get; }
        DbSet<Ambulance> Ambulances { get; }
        DbSet<Doctor> Doctors { get; }
        DbSet<EmergencyRescueRequest> EmergencyRescueRequests { get; }
        DbSet<Pharmacy> Pharmacies { get; }
        DbSet<Deals> Deals { get; }
        DbSet<Payment> Payments { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();
        DbSet<T> Set<T>() where T : class;
    }
} 