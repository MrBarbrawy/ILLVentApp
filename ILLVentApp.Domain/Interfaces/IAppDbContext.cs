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
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();
    }
} 