using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ILLVentApp.Infrastructure.Data.Seeding
{
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RoleSeeder> _logger;

        public RoleSeeder(RoleManager<IdentityRole> roleManager, ILogger<RoleSeeder> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedRolesAsync()
        {
            var roles = new[] { "User", "Admin", "Doctor", "Hospital", "Driver" };

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    var result = await _roleManager.CreateAsync(role);
                    
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Role '{RoleName}' created successfully", roleName);
                    }
                    else
                    {
                        _logger.LogError("Failed to create role '{RoleName}': {Errors}", 
                            roleName, 
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogDebug("Role '{RoleName}' already exists", roleName);
                }
            }
        }
    }
} 