using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ILLVentApp.Domain.Models;
using ILLVentApp.Infrastructure.Data.Contexts;

namespace ILLVentApp.Infrastructure.Data.Seeding
{
    public class ExistingUserRoleMigrator
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ExistingUserRoleMigrator> _logger;

        public ExistingUserRoleMigrator(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext dbContext,
            ILogger<ExistingUserRoleMigrator> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task MigrateExistingUsersAsync()
        {
            _logger.LogInformation("Starting migration of existing users to standard roles...");

            try
            {
                // Get all users who don't have any roles assigned yet
                var usersWithoutRoles = await _dbContext.Users
                    .Where(u => !_dbContext.UserRoles.Any(ur => ur.UserId == u.Id))
                    .ToListAsync();

                _logger.LogInformation("Found {Count} users without standard roles", usersWithoutRoles.Count);

                var migrationResults = new MigrationResults
                {
                    Total = usersWithoutRoles.Count,
                    Successful = 0,
                    Failed = 0,
                    Errors = new List<string>()
                };

                foreach (var user in usersWithoutRoles)
                {
                    try
                    {
                        // Check if custom Role column still exists and has a value
                        var customRole = await GetCustomRoleFromDatabase(user.Id);
                        var roleToAssign = string.IsNullOrEmpty(customRole) ? "User" : customRole;

                        // Ensure the role exists
                        if (!await _roleManager.RoleExistsAsync(roleToAssign))
                        {
                            _logger.LogWarning("Role '{Role}' doesn't exist, assigning 'User' instead for user {UserId}", 
                                roleToAssign, user.Id);
                            roleToAssign = "User";
                        }

                        // Assign the role
                        var result = await _userManager.AddToRoleAsync(user, roleToAssign);
                        
                        if (result.Succeeded)
                        {
                            migrationResults.Successful++;
                            _logger.LogDebug("Successfully assigned role '{Role}' to user {UserId}", roleToAssign, user.Id);
                        }
                        else
                        {
                            migrationResults.Failed++;
                            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                            migrationResults.Errors.Add($"User {user.Id}: {errors}");
                            _logger.LogError("Failed to assign role to user {UserId}: {Errors}", user.Id, errors);
                        }
                    }
                    catch (Exception ex)
                    {
                        migrationResults.Failed++;
                        migrationResults.Errors.Add($"User {user.Id}: {ex.Message}");
                        _logger.LogError(ex, "Exception occurred while migrating user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("Migration completed. Success: {Success}, Failed: {Failed}, Total: {Total}", 
                    migrationResults.Successful, migrationResults.Failed, migrationResults.Total);

                if (migrationResults.Failed > 0)
                {
                    _logger.LogWarning("Migration had {Failed} failures. First few errors: {Errors}", 
                        migrationResults.Failed, 
                        string.Join("; ", migrationResults.Errors.Take(5)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during user role migration");
                throw;
            }
        }

        private async Task<string?> GetCustomRoleFromDatabase(string userId)
        {
            try
            {
                // First check if the Role column exists
                var hasRoleColumn = await CheckIfRoleColumnExists();
                if (!hasRoleColumn)
                {
                    _logger.LogDebug("Role column doesn't exist, skipping custom role lookup");
                    return null;
                }

                // Use direct ADO.NET to avoid EF Core mapping issues
                var connection = _dbContext.Database.GetDbConnection();
                var wasOpen = connection.State == System.Data.ConnectionState.Open;
                
                if (!wasOpen)
                    await connection.OpenAsync();
                
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT Role FROM AspNetUsers WHERE Id = @userId";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@userId";
                    parameter.Value = userId;
                    command.Parameters.Add(parameter);
                    
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString();
                }
                finally
                {
                    if (!wasOpen)
                        await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not retrieve custom role for user {UserId}: {Error}", userId, ex.Message);
                return null;
            }
        }

        private async Task<bool> CheckIfRoleColumnExists()
        {
            try
            {
                var connection = _dbContext.Database.GetDbConnection();
                var wasOpen = connection.State == System.Data.ConnectionState.Open;
                
                if (!wasOpen)
                    await connection.OpenAsync();
                
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Role'";
                    
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
                finally
                {
                    if (!wasOpen)
                        await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not check if Role column exists: {Error}", ex.Message);
                return false;
            }
        }

        private class MigrationResults
        {
            public int Total { get; set; }
            public int Successful { get; set; }
            public int Failed { get; set; }
            public List<string> Errors { get; set; } = new();
        }
    }
} 