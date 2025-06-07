using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using ILLVentApp.Domain.Models;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.DTOs;

namespace ILLVentApp.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [AllowAnonymous] // Client-side authentication handles security
    [Produces("application/json")]
    public class AdminApiController : ControllerBase
    {
        private readonly ILogger<AdminApiController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IProductService _productService;
        private readonly IHospitalService _hospitalService;
        private readonly IPharmacyService _pharmacyService;
        private readonly IMapper _mapper;

        public AdminApiController(
            ILogger<AdminApiController> logger,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IProductService productService,
            IHospitalService hospitalService,
            IPharmacyService pharmacyService,
            IMapper mapper)
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _productService = productService;
            _hospitalService = hospitalService;
            _pharmacyService = pharmacyService;
            _mapper = mapper;
        }

        #region Product Management

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return Ok(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new { success = false, message = "Error retrieving products" });
            }
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var productDto = new ProductDto
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    StockQuantity = request.QuantityInStock,
                    ProductType = request.Category ?? "General",
                    ImageUrl = request.ImageUrl ?? "",
                    Thumbnail = request.ImageUrl ?? "",
                    Rating = 0.0,
                    HasNFC = false,
                    HasMedicalDataStorage = false,
                    HasRescueProtocol = false,
                    HasVitalSensors = false,
                    TechnicalDetails = ""
                };
                
                var result = await _productService.AddProductAsync(productDto);
                
                _logger.LogInformation("Product created successfully: {ProductName}", request.Name);
                return Ok(new { success = true, data = result, message = "Product created successfully" });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { success = false, message = "Error creating product" });
            }
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var productDto = new ProductDto
                {
                    ProductId = id,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    StockQuantity = request.QuantityInStock,
                    ProductType = request.Category ?? "General",
                    ImageUrl = request.ImageUrl ?? "",
                    Thumbnail = request.ImageUrl ?? "",
                    Rating = 0.0,
                    HasNFC = false,
                    HasMedicalDataStorage = false,
                    HasRescueProtocol = false,
                    HasVitalSensors = false,
                    TechnicalDetails = ""
                };
                
                var result = await _productService.UpdateProductAsync(id, productDto);
                
                _logger.LogInformation("Product updated successfully: {ProductId}", id);
                return Ok(new { success = true, data = result, message = "Product updated successfully" });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, new { success = false, message = "Error updating product" });
            }
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                
                _logger.LogInformation("Product deleted successfully: {ProductId}", id);
                return Ok(new { success = true, message = "Product deleted successfully" });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting product" });
            }
        }

        [HttpPost("products/upload-image")]
        public async Task<IActionResult> UploadProductImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No image file provided" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(image.ContentType.ToLower()))
                {
                    return BadRequest(new { success = false, message = "Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed." });
                }

                // Validate file size (max 5MB)
                if (image.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "File size too large. Maximum size is 5MB." });
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(image.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Return the URL
                var imageUrl = $"/uploads/products/{fileName}";
                
                _logger.LogInformation("Product image uploaded successfully: {FileName}", fileName);
                return Ok(new { success = true, imageUrl = imageUrl, message = "Image uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image");
                return StatusCode(500, new { success = false, message = "Error uploading image" });
            }
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new { success = false, message = "Product not found" });
                }
                
                return Ok(new { success = true, data = product });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving product" });
            }
        }

        #endregion

        #region User Management

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = _userManager.Users.Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.Surname,
                    u.CreatedAt,
                    u.IsEmailVerified
                }).ToList();

                // Get roles for each user
                var usersWithRoles = new List<object>();
                foreach (var user in users)
                {
                    var userEntity = await _userManager.FindByIdAsync(user.Id);
                    var roles = await _userManager.GetRolesAsync(userEntity);
                    usersWithRoles.Add(new
                    {
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.Surname,
                        user.CreatedAt,
                        user.IsEmailVerified,
                        Roles = roles
                    });
                }

                return Ok(new { success = true, data = usersWithRoles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { success = false, message = "Error retrieving users" });
            }
        }

        [HttpPost("users/{userId}/roles")]
        public async Task<IActionResult> AssignRole(string userId, [FromBody] AdminAssignRoleRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                if (!await _roleManager.RoleExistsAsync(request.RoleName))
                    return BadRequest(new { success = false, message = "Role does not exist" });

                var result = await _userManager.AddToRoleAsync(user, request.RoleName);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Role {RoleName} assigned to user {UserId}", request.RoleName, userId);
                    return Ok(new { success = true, message = $"Role {request.RoleName} assigned successfully" });
                }

                return BadRequest(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Error assigning role" });
            }
        }

        [HttpDelete("users/{userId}/roles/{roleName}")]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Role {RoleName} removed from user {UserId}", roleName, userId);
                    return Ok(new { success = true, message = $"Role {roleName} removed successfully" });
                }

                return BadRequest(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Error removing role" });
            }
        }

        [HttpGet("roles")]
        public IActionResult GetRoles()
        {
            try
            {
                var roles = _roleManager.Roles.Select(r => r.Name).ToList();
                return Ok(new { success = true, data = roles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new { success = false, message = "Error retrieving roles" });
            }
        }

        #endregion

        #region Hospital Management

        [HttpPost("hospitals/upload-image")]
        public async Task<IActionResult> UploadHospitalImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No image file provided" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(image.ContentType.ToLower()))
                {
                    return BadRequest(new { success = false, message = "Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed." });
                }

                // Validate file size (max 5MB)
                if (image.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "File size too large. Maximum size is 5MB." });
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "hospitals");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(image.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Return the URL
                var imageUrl = $"/uploads/hospitals/{fileName}";
                
                _logger.LogInformation("Hospital image uploaded successfully: {FileName}", fileName);
                return Ok(new { success = true, imageUrl = imageUrl, message = "Image uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading hospital image");
                return StatusCode(500, new { success = false, message = "Error uploading image" });
            }
        }

        [HttpGet("hospitals")]
        public async Task<IActionResult> GetHospitals()
        {
            try
            {
                var hospitals = await _hospitalService.GetAllHospitalsAsync();
                
                // Map to a simpler format for admin display
                var hospitalList = hospitals.Select(h => new
                {
                    id = h.HospitalId,
                    name = h.Name,
                    address = h.Location,
                    phone = h.ContactNumber,
                    email = "N/A" // Email not available in current DTO
                }).ToList();
                
                return Ok(new { success = true, data = hospitalList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving hospitals");
                return StatusCode(500, new { success = false, message = "Error retrieving hospitals" });
            }
        }

        [HttpPost("hospitals")]
        public async Task<IActionResult> CreateHospital([FromBody] CreateHospitalRequest request)
        {
            try
            {
                _logger.LogInformation("Hospital creation request received: {HospitalName}", request.Name);
                
                var createHospitalDto = new CreateHospitalDto
                {
                    Name = request.Name,
                    Description = request.Description ?? $"Hospital located in {request.Address}",
                    Location = request.Address,
                    ContactNumber = request.Phone,
                    Established = request.Established ?? DateTime.Now.Year.ToString(),
                    Specialties = request.Specialties ?? new List<string> { "General Medicine", "Emergency Care" },
                    IsAvailable = request.IsAvailable,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    HasContract = request.HasContract,
                    Rating = request.Rating,
                    ImageUrl = request.ImageUrl,
                    Thumbnail = request.ImageUrl // Use same image for thumbnail
                };

                var result = await _hospitalService.CreateHospitalAsync(createHospitalDto);
                
                return Ok(new { 
                    success = true, 
                    message = "Hospital created successfully", 
                    data = new {
                        id = result.HospitalId,
                        name = result.Name,
                        address = result.Location,
                        phone = result.ContactNumber,
                        email = request.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating hospital");
                return StatusCode(500, new { success = false, message = "Error creating hospital" });
            }
        }

        [HttpDelete("hospitals/{id}")]
        public async Task<IActionResult> DeleteHospital(int id)
        {
            try
            {
                _logger.LogInformation("Hospital deletion request received: {HospitalId}", id);
                
                var result = await _hospitalService.DeleteHospitalAsync(id);
                
                if (result)
                {
                    return Ok(new { success = true, message = "Hospital deleted successfully" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Hospital not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting hospital {HospitalId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting hospital" });
            }
        }

        #endregion

        #region Pharmacy Management

        [HttpPost("pharmacies/upload-image")]
        public async Task<IActionResult> UploadPharmacyImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No image file provided" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(image.ContentType.ToLower()))
                {
                    return BadRequest(new { success = false, message = "Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed." });
                }

                // Validate file size (max 5MB)
                if (image.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "File size too large. Maximum size is 5MB." });
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pharmacies");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(image.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Return the URL
                var imageUrl = $"/uploads/pharmacies/{fileName}";
                
                _logger.LogInformation("Pharmacy image uploaded successfully: {FileName}", fileName);
                return Ok(new { success = true, imageUrl = imageUrl, message = "Image uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading pharmacy image");
                return StatusCode(500, new { success = false, message = "Error uploading image" });
            }
        }

        [HttpGet("pharmacies")]
        public async Task<IActionResult> GetPharmacies()
        {
            try
            {
                var pharmacies = await _pharmacyService.GetAllPharmaciesAsync();
                
                // Map to a simpler format for admin display
                var pharmacyList = pharmacies.Select(p => new
                {
                    id = p.PharmacyId,
                    name = p.Name,
                    address = p.Location,
                    phone = p.ContactNumber,
                    rating = p.Rating,
                    hasContract = p.HasContract,
                    acceptsInsurance = p.AcceptPrivateInsurance
                }).ToList();
                
                return Ok(new { success = true, data = pharmacyList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pharmacies");
                return StatusCode(500, new { success = false, message = "Error retrieving pharmacies" });
            }
        }

        [HttpPost("pharmacies")]
        public async Task<IActionResult> CreatePharmacy([FromBody] CreatePharmacyRequest request)
        {
            try
            {
                _logger.LogInformation("Pharmacy creation request received: {PharmacyName}", request.Name);
                
                var createPharmacyDto = new CreatePharmacyDto
                {
                    Name = request.Name,
                    Description = request.Description ?? $"Pharmacy located in {request.Address}",
                    Location = request.Address,
                    ContactNumber = request.Phone,
                    Rating = request.Rating,
                    AcceptPrivateInsurance = request.AcceptPrivateInsurance,
                    HasContract = request.HasContract,
                    ImageUrl = request.ImageUrl,
                    Thumbnail = request.ImageUrl // Use same image for thumbnail
                };

                var result = await _pharmacyService.CreatePharmacyAsync(createPharmacyDto);
                
                return Ok(new { 
                    success = true, 
                    message = "Pharmacy created successfully", 
                    data = new {
                        id = result.PharmacyId,
                        name = result.Name,
                        address = result.Location,
                        phone = result.ContactNumber,
                        rating = result.Rating
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pharmacy");
                return StatusCode(500, new { success = false, message = "Error creating pharmacy" });
            }
        }

        [HttpDelete("pharmacies/{id}")]
        public async Task<IActionResult> DeletePharmacy(int id)
        {
            try
            {
                _logger.LogInformation("Pharmacy deletion request received: {PharmacyId}", id);
                
                var result = await _pharmacyService.DeletePharmacyAsync(id);
                
                if (result)
                {
                    return Ok(new { success = true, message = "Pharmacy deleted successfully" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Pharmacy not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pharmacy {PharmacyId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting pharmacy" });
            }
        }

        #endregion

        #region System Logs

        [HttpGet("logs")]
        public IActionResult GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // This is a placeholder - you'll need to implement actual log reading
                // You can read from log files, database logs, or use a logging framework like Serilog
                var logs = new List<object>
                {
                    new { Timestamp = DateTime.UtcNow, Level = "Info", Message = "System started", Category = "System" },
                    new { Timestamp = DateTime.UtcNow.AddMinutes(-5), Level = "Warning", Message = "High memory usage detected", Category = "Performance" },
                    new { Timestamp = DateTime.UtcNow.AddMinutes(-10), Level = "Error", Message = "Database connection timeout", Category = "Database" }
                };

                return Ok(new { success = true, data = logs, page, pageSize, total = logs.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return StatusCode(500, new { success = false, message = "Error retrieving logs" });
            }
        }

        #endregion
    }

    // Request DTOs
    public class AdminAssignRoleRequest
    {
        public string RoleName { get; set; }
    }

    public class CreateProductRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
    }

    public class UpdateProductRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
    }

    public class CreateHospitalRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string? Established { get; set; }
        public List<string>? Specialties { get; set; }
        public bool IsAvailable { get; set; } = true;
        public double Latitude { get; set; } = 0.0;
        public double Longitude { get; set; } = 0.0;
        public bool HasContract { get; set; } = false;
        public double Rating { get; set; } = 0.0;
        public string? ImageUrl { get; set; }
    }

    public class CreatePharmacyRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public double Rating { get; set; } = 0.0;
        public bool AcceptPrivateInsurance { get; set; } = false;
        public bool HasContract { get; set; } = false;
        public string? ImageUrl { get; set; }
    }
} 