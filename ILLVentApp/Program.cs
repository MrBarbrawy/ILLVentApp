using ILLVentApp.Application.Mappings;
using ILLVentApp.Application.Services;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using ILLVentApp.Infrastructure.Configuration;
using ILLVentApp.Infrastructure.Data.Contexts;
using ILLVentApp.Infrastructure.Data.Seeding;
using ILLVentApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using ILLVentApp.Application.Interfaces;
using Stripe;


namespace ILLVentApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
            builder.Services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });



            // Configure JSON response format
            builder.Services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new ProducesAttribute("application/json"));
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add DbContext
			builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register IAppDbContext to use AppDbContext
            builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

			builder.Services.AddIdentity<User, IdentityRole>(options =>
			{
                // Password settings
				options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
				options.User.RequireUniqueEmail = true;
			})
				.AddEntityFrameworkStores<AppDbContext>()
            .AddUserManager<CustomUserManager>()
                .AddDefaultTokenProviders();

            // Add JWT Authentication
			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
            .AddJwtBearer(options =>
            {
            	options.TokenValidationParameters = new TokenValidationParameters
            	{
            		ValidateIssuer = true,
            		ValidateAudience = true,
                    ValidateLifetime = false,
            		ValidateIssuerSigningKey = true,
            		ValidIssuer = builder.Configuration["Jwt:Issuer"],
            		ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
                    // Add more lenient validation options
                    NameClaimType = "sub", // Make sure sub claim is mapped to name
                    RoleClaimType = "role"
            	};
                
                // Add event handlers for more detailed logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError("Authentication failed: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Token validated successfully");
                        return Task.CompletedTask;
                    }
                };
            });

			builder.Services.Configure<EmailSettings>(
			builder.Configuration.GetSection(EmailSettings.SectionName));

            // Configure Stripe
            StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

            builder.Services.AddAutoMapper(typeof(AuthProfile), typeof(ProductProfile), typeof(OrderProfile));

            builder.Services.AddHttpContextAccessor();

            // Add logging
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });

            // Get encryption key for QR code service
            var encryptionKey = builder.Configuration["Security:EncryptionKey"] ?? "Default32CharEncryptionKeyForDevOnly!";

			builder.Services
           .AddScoped<IAuthService, AuthService>()
		   .AddScoped<IJwtService, JwtService>()
		   .AddScoped<IEmailService, EmailService>()
		   .AddScoped<IOtpService, OtpService>()
           .AddScoped<IUserFriendlyIdService, UserFriendlyIdService>()
           .AddScoped<IHospitalService, HospitalService>()
           .AddScoped<IPharmacyService, PharmacyService>()
           .AddScoped<IDoctorService, DoctorService>()
           .AddScoped<IProductService,Application.Services.ProductService>()
           .AddScoped<ICartService, CartService>()
           .AddScoped<IOrderService, OrderService>()
           .AddScoped<IQrCodeService>(provider => 
               new QrCodeService(
                   provider.GetRequiredService<ILogger<QrCodeService>>(),
                   encryptionKey))
           .AddScoped<IMedicalHistoryService>(provider => 
               new MedicalHistoryService(
                   provider.GetRequiredService<IAppDbContext>(),
                   provider.GetRequiredService<IQrCodeService>(),
                   provider.GetRequiredService<ILogger<MedicalHistoryService>>(),
                   encryptionKey))
		   .AddSingleton<IDistributedLockProvider, LocalLockProvider>()
		   .AddSingleton<IRetryPolicyProvider, RetryPolicyProvider>();

			builder.Services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "IllVent API", Version = "v1" });

				// Add JWT Bearer authentication support
				c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Description = "JWT Authorization header using the Bearer scheme",
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.Http,
					Scheme = "bearer"
				});

				c.OperationFilter<SecurityRequirementsOperationFilter>();
			});

			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAll", policy =>
				{
					policy.AllowAnyOrigin()
						  .AllowAnyMethod()
						  .AllowAnyHeader();
				});
			});

			builder.Services.AddControllers()
                // Explicitly register the controller assembly to ensure discovery
                .AddApplicationPart(typeof(Controllers.MedicalHistoryController).Assembly);
                
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

				app.UseSwagger();
				app.UseSwaggerUI(c =>
				{
					c.SwaggerEndpoint("/swagger/v1/swagger.json", "IllVent API v1");
				});
                
                // Seed data in development
                using (var scope = app.Services.CreateScope())
                {
                    var serviceProvider = scope.ServiceProvider;
                    try
                    {
                         // Seed roles first
                         var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                         var logger = serviceProvider.GetRequiredService<ILogger<ILLVentApp.Infrastructure.Data.Seeding.RoleSeeder>>();
                         var roleSeeder = new ILLVentApp.Infrastructure.Data.Seeding.RoleSeeder(roleManager, logger);
                         roleSeeder.SeedRolesAsync().Wait();
                         
                         // Migrate existing users to standard roles
                         var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
                         var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
                         var migrationLogger = serviceProvider.GetRequiredService<ILogger<ILLVentApp.Infrastructure.Data.Seeding.ExistingUserRoleMigrator>>();
                         var userMigrator = new ILLVentApp.Infrastructure.Data.Seeding.ExistingUserRoleMigrator(userManager, roleManager, dbContext, migrationLogger);
                         userMigrator.MigrateExistingUsersAsync().Wait();
                         
                         ILLVentApp.Infrastructure.Data.Seeding.HospitalDataSeeder.SeedHospitalData(serviceProvider);
                         ILLVentApp.Infrastructure.Data.Seeding.HospitalImageSeeder.SeedHospitalImages(serviceProvider);
                         ILLVentApp.Infrastructure.Data.Seeding.PharmacyDataSeeder.SeedPharmacyData(serviceProvider);
                         ILLVentApp.Infrastructure.Data.Seeding.PharmacyImageSeeder.SeedPharmacyImages(serviceProvider);
                         ILLVentApp.Infrastructure.Data.Seeding.DoctorDataSeeder.SeedDoctorData(serviceProvider);
                         ILLVentApp.Infrastructure.Data.Seeding.DoctorImageSeeder.SeedDoctorImages(serviceProvider);
                         
                         // Seed product data
                         var dbContext2 = serviceProvider.GetRequiredService<IAppDbContext>();
                         ProductSeeder.SeedProductsAsync(dbContext2).Wait();
                    }
                    catch (Exception ex)
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "An error occurred while seeding the database.");
                    }
                }
			}

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseCors("AllowAll");

            // Add routing middleware first
            app.UseRouting();

            // Then authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Add endpoints middleware with default MVC route
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            app.Run();
        }
    }
}
