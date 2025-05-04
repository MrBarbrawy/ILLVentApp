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
using System.Text.Json;
using ILLVentApp.Infrastructure.Data.Seeding;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
		public DbSet<Schedule> Schedules { get; set; }
		public DbSet<TimeSlot> TimeSlots { get; set; }
		public DbSet<Appointment> Appointments { get; set; }
		public DbSet<Deals> Deals { get; set; }
		public DbSet<Payment> Payments { get; set; }
		public DbSet<MedicalHistory> MedicalHistories { get; set; }
		public DbSet<MedicalCondition> MedicalConditions { get; set; }
		public DbSet<FamilyHistory> FamilyHistories { get; set; }
		public DbSet<SurgicalHistory> SurgicalHistories { get; set; }
		public DbSet<ImmunizationHistory> ImmunizationHistories { get; set; }
		public DbSet<SocialHistory> SocialHistories { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<OrderItem> OrderItems { get; set; }
		public DbSet<CartItem> CartItems { get; set; }

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
			ConfigureProduct(modelBuilder);
			ConfigureOrder(modelBuilder);
			ConfigureOrderItem(modelBuilder);
			ConfigureCartItem(modelBuilder);

			modelBuilder.Entity<User>(entity =>
			{
				entity.HasIndex(u => u.NormalizedEmail)
					.IsUnique()
					.HasFilter("[NormalizedEmail] IS NOT NULL");
			});
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
				entity.HasKey(m => m.MedicalHistoryId);
				
				// User relationship - keep as Cascade
				entity.HasOne(m => m.User)
					  .WithOne(u => u.MedicalHistory)
					  .HasForeignKey<MedicalHistory>(m => m.UserId)
					  .OnDelete(DeleteBehavior.Cascade);

				// One-to-many relationships - use NoAction
				entity.HasMany(m => m.MedicalConditions)
					  .WithOne()
					  .HasForeignKey(mc => mc.MedicalHistoryId)
					  .OnDelete(DeleteBehavior.NoAction);

				entity.HasMany(m => m.FamilyHistory)
					  .WithOne(fh => fh.MedicalHistory)
					  .HasForeignKey(fh => fh.MedicalHistoryId)
					  .OnDelete(DeleteBehavior.NoAction);

				entity.HasMany(m => m.SurgicalHistories)
					  .WithOne()
					  .HasForeignKey(sh => sh.MedicalHistoryId)
					  .OnDelete(DeleteBehavior.NoAction);

				// One-to-one relationships - use NoAction
				entity.HasOne(m => m.ImmunizationHistory)
					  .WithOne()
					  .HasForeignKey<ImmunizationHistory>(ih => ih.MedicalHistoryId)
					  .OnDelete(DeleteBehavior.NoAction);

				entity.HasOne(m => m.SocialHistory)
					  .WithOne()
					  .HasForeignKey<SocialHistory>(sh => sh.MedicalHistoryId)
					  .OnDelete(DeleteBehavior.NoAction);
			});

			// Configure FamilyHistory separately to handle the bidirectional relationship
			modelBuilder.Entity<FamilyHistory>(entity =>
			{
				entity.HasKey(fh => fh.Id);
				entity.HasOne(fh => fh.MedicalHistory)
					  .WithMany(mh => mh.FamilyHistory)
					  .HasForeignKey(fh => fh.MedicalHistoryId)
					  .OnDelete(DeleteBehavior.NoAction);
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
				
				entity.Property(h => h.Name)
					.IsRequired()
					.HasMaxLength(100);

				entity.Property(h => h.Description)
					.HasMaxLength(500);

				entity.Property(h => h.Location)
					.IsRequired()
					.HasMaxLength(200);

				entity.Property(h => h.ContactNumber)
					.IsRequired()
					.HasMaxLength(20);

				entity.Property(h => h.Established)
					.HasMaxLength(50);

				// Configure Specialties as a JSON column
				entity.Property(h => h.Specialties)
					.HasConversion(
						v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
						v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
					);

				// Configure relationships
				entity.HasMany(h => h.Ambulances)
					.WithOne(a => a.Hospital)
					.HasForeignKey(a => a.HospitalId)
					.OnDelete(DeleteBehavior.Cascade);
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
				
				entity.Property(p => p.Name)
					.IsRequired()
					.HasMaxLength(100);

				entity.Property(p => p.Description)
					.HasMaxLength(500);

				entity.Property(p => p.Location)
					.IsRequired()
					.HasMaxLength(200);

				entity.Property(p => p.ContactNumber)
					.IsRequired()
					.HasMaxLength(20);
			});
		}

		private void ConfigureDoctor(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Doctor>(entity =>
			{
				entity.HasKey(d => d.DoctorId);

				// Required fields configuration
				entity.Property(d => d.Name)
					.IsRequired()
					.HasMaxLength(100);

				entity.Property(d => d.Specialty)
					.IsRequired()
					.HasMaxLength(100);

				entity.Property(d => d.Education)
					.HasMaxLength(500);

				entity.Property(d => d.Hospital)
					.HasMaxLength(100);

				entity.Property(d => d.ImageUrl)
					.HasMaxLength(255);

				entity.Property(d => d.Thumbnail)
					.HasMaxLength(255);

				entity.Property(d => d.Location)
					.HasMaxLength(500);

				entity.Property(d => d.Rating)
					.HasDefaultValue(0.0);

				entity.Property(d => d.AcceptInsurance)
					.HasDefaultValue(false);

				// Configure WorkingDays as a string
				entity.Property(d => d.WorkingDays)
					.IsRequired()
					.HasMaxLength(20);  // Enough for "0,1,2,3,4,5,6"

				// Relationships
				entity.HasMany(d => d.Schedules)
					.WithOne(s => s.Doctor)
					.HasForeignKey(s => s.DoctorId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasMany(d => d.Appointments)
					.WithOne(a => a.Doctor)
					.HasForeignKey(a => a.DoctorId)
					.OnDelete(DeleteBehavior.Cascade);
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

		private void ConfigureProduct(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Product>(entity =>
			{
				entity.HasKey(p => p.ProductId);
				
				entity.Property(p => p.Name)
					.IsRequired()
					.HasMaxLength(100);

				entity.Property(p => p.Description)
					.IsRequired()
					.HasMaxLength(500);

				entity.Property(p => p.Price)
					.IsRequired()
					.HasColumnType("decimal(18,2)");

				entity.Property(p => p.ImageUrl)
					.HasMaxLength(255);

				entity.Property(p => p.Thumbnail)
					.HasMaxLength(255);

				entity.Property(p => p.Rating)
					.HasDefaultValue(0.0);

				entity.Property(p => p.ProductType)
					.IsRequired()
					.HasMaxLength(50);

				entity.Property(p => p.TechnicalDetails)
					.HasMaxLength(1000);

				entity.Property(p => p.CreatedAt)
					.IsRequired()
					.HasDefaultValueSql("GETUTCDATE()");

				entity.Property(p => p.UpdatedAt)
					.IsRequired()
					.HasDefaultValueSql("GETUTCDATE()");
			});
		}

		private void ConfigureOrder(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Order>(entity =>
			{
				entity.HasKey(o => o.OrderId);
				
				entity.HasOne(o => o.User)
					.WithMany()
					.HasForeignKey(o => o.UserId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.Property(o => o.OrderDate)
					.IsRequired()
					.HasDefaultValueSql("GETUTCDATE()");

				entity.Property(o => o.TotalAmount)
					.IsRequired()
					.HasColumnType("decimal(18,2)");

				entity.Property(o => o.PaymentMethod)
					.IsRequired()
					.HasMaxLength(20);

				entity.Property(o => o.PaymentStatus)
					.IsRequired()
					.HasMaxLength(20)
					.HasDefaultValue("Pending");

				entity.Property(o => o.ShippingAddress)
					.IsRequired()
					.HasMaxLength(500);

				entity.Property(o => o.ShippingCost)
					.IsRequired()
					.HasColumnType("decimal(18,2)");

				entity.Property(o => o.OrderStatus)
					.IsRequired()
					.HasMaxLength(20)
					.HasDefaultValue("Pending");

				entity.Property(o => o.StripeSessionId)
					.HasMaxLength(100)
					.IsRequired(false);

				entity.Property(o => o.StripePaymentIntentId)
					.HasMaxLength(100)
					.IsRequired(false);
			});
		}

		private void ConfigureOrderItem(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<OrderItem>(entity =>
			{
				entity.HasKey(oi => oi.OrderItemId);
				
				entity.HasOne(oi => oi.Order)
					.WithMany(o => o.OrderItems)
					.HasForeignKey(oi => oi.OrderId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(oi => oi.Product)
					.WithMany(p => p.OrderItems)
					.HasForeignKey(oi => oi.ProductId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.Property(oi => oi.Quantity)
					.IsRequired();

				entity.Property(oi => oi.UnitPrice)
					.IsRequired()
					.HasColumnType("decimal(18,2)");

				entity.Property(oi => oi.TotalPrice)
					.IsRequired()
					.HasColumnType("decimal(18,2)");
			});
		}

		private void ConfigureCartItem(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<CartItem>(entity =>
			{
				entity.HasKey(ci => ci.CartItemId);
				
				entity.HasOne(ci => ci.User)
					.WithMany()
					.HasForeignKey(ci => ci.UserId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(ci => ci.Product)
					.WithMany()
					.HasForeignKey(ci => ci.ProductId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.Property(ci => ci.Quantity)
					.IsRequired();

				entity.Property(ci => ci.CreatedAt)
					.IsRequired()
					.HasDefaultValueSql("GETUTCDATE()");

				entity.Property(ci => ci.UpdatedAt)
					.IsRequired()
					.HasDefaultValueSql("GETUTCDATE()");
			});
		}

		public async Task<int> SaveChangesAsync()
		{
			return await base.SaveChangesAsync();
		}

		public async Task<IDbContextTransaction> BeginTransactionAsync()
		{
			return await Database.BeginTransactionAsync();
		}

		public new EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class
		{
			return base.Entry(entity);
		}
	}
}
