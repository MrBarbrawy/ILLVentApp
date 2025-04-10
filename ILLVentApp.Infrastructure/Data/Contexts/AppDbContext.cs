using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ILLVentApp.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ILLVentApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace ILLVentApp.Infrastructure.Data.Contexts
{
	public class AppDbContext : IdentityDbContext<User>, IAppDbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

		public DbSet<User> Users { get; set; }
		public DbSet<EmergencyRescueRequest> EmergencyRescueRequests { get; set; }
		public DbSet<Hospital> Hospitals { get; set; }
		public DbSet<Ambulance> Ambulances { get; set; }
		public DbSet<Driver> Drivers { get; set; }
		public DbSet<Pharmacy> Pharmacies { get; set; }
		public DbSet<Doctor> Doctors { get; set; }
		public DbSet<Deals> Deals { get; set; }
		public DbSet<Payment> Payments { get; set; }
		public DbSet<MedicalHistory> MedicalHistories { get; set; }
		public DbSet<MedicalCondition> MedicalConditions { get; set; }
		public DbSet<FamilyHistory> FamilyHistories { get; set; }
		public DbSet<SurgicalHistory> SurgicalHistories { get; set; }
		public DbSet<ImmunizationHistory> ImmunizationHistories { get; set; }
		public DbSet<SocialHistory> SocialHistories { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Configure relationships and constraints
			ConfigureUser(modelBuilder);
			ConfigureMedicalHistory(modelBuilder);
			ConfigureEmergencyRescueRequest(modelBuilder);
			ConfigureHospital(modelBuilder);
			ConfigureAmbulance(modelBuilder);
			ConfigurePharmacy(modelBuilder);
			ConfigureDoctor(modelBuilder);
			ConfigureDeals(modelBuilder);
			ConfigurePayment(modelBuilder);
			modelBuilder.Entity<User>(entity =>
			{
				entity.HasIndex(u => u.NormalizedEmail)
					.IsUnique()
					.HasFilter("[NormalizedEmail] IS NOT NULL");
			});

			modelBuilder.Entity<MedicalHistory>()
				.HasMany(m => m.MedicalConditions)
				.WithOne()
				.HasForeignKey(mc => mc.MedicalHistoryId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<MedicalHistory>()
				.HasMany(m => m.FamilyHistory)
				.WithOne()
				.HasForeignKey(fh => fh.MedicalHistoryId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<MedicalHistory>()
				.HasMany(m => m.SurgicalHistories)
				.WithOne()
				.HasForeignKey(sh => sh.MedicalHistoryId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<MedicalHistory>()
				.HasOne(m => m.ImmunizationHistory)
				.WithOne()
				.HasForeignKey<ImmunizationHistory>(ih => ih.MedicalHistoryId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<MedicalHistory>()
				.HasOne(m => m.SocialHistory)
				.WithOne()
				.HasForeignKey<SocialHistory>(sh => sh.MedicalHistoryId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		private void ConfigureUser(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>(entity =>
			{
				// Required fields configuration
				entity.Property(u => u.FirstName)
					.IsRequired()
					.HasMaxLength(50);

				entity.Property(u => u.Surname)
					.IsRequired()
					.HasMaxLength(50);

				entity.Property(u => u.QrCode)
					.IsRequired()
					.HasMaxLength(255)
					.HasDefaultValueSql("NEWID()"); // Auto-generate if using SQL Server

				entity.Property(u => u.Address)
					.IsRequired()
					.HasMaxLength(500)
					.HasDefaultValue("Pending");

				entity.Property(u => u.Role)
					.IsRequired()
					.HasMaxLength(20)
					.HasDefaultValue("User");

				entity.Property(u => u.IsEmailVerified)
					.IsRequired()
					.HasDefaultValue(false);

				entity.Property(u => u.CreatedAt)
					.IsRequired()
					.HasDefaultValueSql("GETUTCDATE()");

				entity.Property(u => u.SecurityVersion)
					.IsRequired()
					.HasDefaultValue(1);

				// Indexes
				entity.HasIndex(u => u.QrCode).IsUnique();
				entity.HasIndex(u => u.NormalizedEmail).IsUnique();

				entity.Property(u => u.UserName)
				   .IsRequired()
				   .HasMaxLength(256);

				entity.Property(u => u.NormalizedUserName)
				   .IsRequired()
				   .HasMaxLength(256);
			});
		}

		private void ConfigureMedicalHistory(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<MedicalHistory>(entity =>
			{
				entity.HasKey(m => m.Id);
				entity.HasOne(m => m.User)
					  .WithOne(u => u.MedicalHistory)
					  .HasForeignKey<MedicalHistory>(m => m.UserId);

				// Basic Info

				entity.Property(m => m.Address)
					  .IsRequired()
					  .HasMaxLength(200);

				entity.Property(m => m.BloodType)
					  .IsRequired()
					  .HasMaxLength(3);

				entity.Property(m => m.Age)
					  .IsRequired();

				entity.Property(m => m.Weight)
					  .IsRequired()
					  .HasColumnType("decimal(5,2)"); // For storing weight in kg with 2 decimal places

				entity.Property(m => m.Height)
					  .IsRequired()
					  .HasColumnType("decimal(5,2)"); // For storing height in cm with 2 decimal places

				entity.Property(m => m.Gender)
					  .IsRequired()
					  .HasMaxLength(10);

				// Medical Conditions
				entity.Property(m => m.HasHighBloodPressure)
					  .IsRequired();

				entity.Property(m => m.HasLowBloodPressure)
					  .IsRequired();

				entity.Property(m => m.HasDiabetes)
					  .IsRequired();

				entity.Property(m => m.DiabetesType)
					  .HasMaxLength(20);

				entity.Property(m => m.HasAllergies)
					  .IsRequired();

				entity.Property(m => m.AllergiesDetails)
					  .HasMaxLength(500);

				entity.Property(m => m.HasSurgeryHistory)
					  .IsRequired();

				entity.Property(m => m.BirthControlMethod)
					  .HasMaxLength(100);

				entity.Property(m => m.HasBloodTransfusionObjection)
					  .IsRequired();

				// QR Code
				entity.Property(m => m.QrCode)
					  .HasMaxLength(255);

				entity.Property(m => m.QrCodeGeneratedAt);

				entity.Property(m => m.QrCodeExpiresAt);
			});

			// Configure MedicalCondition
			modelBuilder.Entity<MedicalCondition>(entity =>
			{
				entity.HasKey(mc => mc.Id);
				entity.Property(mc => mc.Condition)
					  .IsRequired()
					  .HasMaxLength(100);
				entity.Property(mc => mc.Details)
					  .HasMaxLength(500);
			});

			// Configure FamilyHistory
		

			// Configure SurgicalHistory
			modelBuilder.Entity<SurgicalHistory>(entity =>
			{
				entity.HasKey(sh => sh.Id);
				entity.Property(sh => sh.SurgeryType)
					  .IsRequired()
					  .HasMaxLength(100);
				entity.Property(sh => sh.Details)
					  .HasMaxLength(500);
			});

			// Configure ImmunizationHistory
			modelBuilder.Entity<ImmunizationHistory>(entity =>
			{
				entity.HasKey(ih => ih.Id);
			});

			// Configure SocialHistory
			modelBuilder.Entity<SocialHistory>(entity =>
			{
				entity.HasKey(sh => sh.Id);
				entity.Property(sh => sh.ExerciseType)
					  .HasMaxLength(50);
				entity.Property(sh => sh.ExerciseFrequency)
					  .HasMaxLength(50);
			});
		}

		private void ConfigureEmergencyRescueRequest(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<EmergencyRescueRequest>(entity =>
			{
				entity.HasKey(e => e.RequestId);
				entity.HasOne(e => e.User)
					  .WithMany(u => u.EmergencyRescueRequests)
					  .HasForeignKey(e => e.UserId);

				entity.HasOne(e => e.AcceptedHospital)
					  .WithMany()
					  .HasForeignKey(e => e.AcceptedHospitalId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.Property(e => e.RequestDescription).IsRequired();
				entity.Property(e => e.RequestImage).HasMaxLength(255); // Encrypted URL
				entity.Property(e => e.RequestStatus).IsRequired();
				entity.Property(e => e.InjuryDescription).IsRequired(); // Encrypted
				entity.Property(e => e.InjuryPhotoUrl).HasMaxLength(255); // Encrypted URL
				entity.Property(e => e.Timestamp).IsRequired();
			});
		}

		private void ConfigureHospital(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Hospital>(entity =>
			{
				entity.HasKey(h => h.HospitalId);
				entity.Property(h => h.Name).IsRequired().HasMaxLength(100);
				entity.Property(h => h.Description);
				entity.Property(h => h.Latitude).IsRequired();
				entity.Property(h => h.Longitude).IsRequired();
				entity.Property(h => h.Phone).IsRequired().HasMaxLength(20);
				entity.Property(h => h.HasContract).IsRequired();
			});
		}

		private void ConfigureAmbulance(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Ambulance>(entity =>
			{
				entity.HasKey(a => a.AmbulanceId);
				entity.HasOne(a => a.Driver)
					  .WithMany(d => d.Ambulances) 
					  .HasForeignKey(a => a.DriverId);

				entity.HasOne(a => a.Hospital)
					  .WithMany(h => h.Ambulances)
					  .HasForeignKey(a => a.HospitalId);

				entity.Property(a => a.AmbulancePlateNumber).IsRequired().HasMaxLength(20);
				entity.Property(a => a.Capacity).IsRequired();
				entity.Property(a => a.Equipments).IsRequired();
			});
		}

		private void ConfigurePharmacy(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Pharmacy>(entity =>
			{
				entity.HasKey(p => p.PharmacyId);
				entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
				entity.Property(p => p.Description);
				entity.Property(p => p.Latitude).IsRequired();
				entity.Property(p => p.Longitude).IsRequired();
				entity.Property(p => p.Phone).IsRequired().HasMaxLength(20);
				entity.Property(p => p.HasContract).IsRequired();
			});
		}

		private void ConfigureDoctor(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Doctor>(entity =>
			{
				entity.HasKey(d => d.DoctorId);
				entity.HasOne(d => d.Hospital)
					  .WithMany(h => h.Doctors)
					  .HasForeignKey(d => d.HospitalId);

				entity.Property(d => d.DoctorName).IsRequired().HasMaxLength(50);
				entity.Property(d => d.Specialization).IsRequired().HasMaxLength(100);
				entity.Property(d => d.Phone).IsRequired().HasMaxLength(20);
				entity.Property(d => d.Email).IsRequired().HasMaxLength(100);
			});
		}

		private void ConfigureDeals(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Deals>(entity =>
			{
				entity.HasKey(d => d.DealId);
				entity.HasOne(d => d.Pharmacy)
			    .WithMany(p => p.Deals) // Pharmacy has a List<Deals> Deals
			    .HasForeignKey(d => d.PharmacyId);
				entity.Property(d => d.DealType).IsRequired();
				entity.Property(d => d.DealDetails).IsRequired();
				entity.Property(d => d.ExpirationDate).IsRequired();
			});
		}

		private void ConfigurePayment(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Payment>(entity =>
			{
				entity.HasKey(p => p.PaymentId);
				entity.HasOne(p => p.User)
					  .WithMany()
					  .HasForeignKey(p => p.UserId);

				entity.HasOne(p => p.Deal)
					  .WithMany()
					  .HasForeignKey(p => p.DealId);

				entity.Property(p => p.Amount).IsRequired();
				entity.Property(p => p.PaymentMethod).IsRequired();
				entity.Property(p => p.PaymentDate).IsRequired();
			});
		}

		public async Task<IDbContextTransaction> BeginTransactionAsync()
		{
			return await Database.BeginTransactionAsync();
		}
	}
}
