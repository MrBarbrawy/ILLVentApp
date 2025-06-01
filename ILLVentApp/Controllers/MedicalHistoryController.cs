using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace ILLVentApp.Controllers
{
    [ApiController]
    [Route("api/medicalhistory")]
    public class MedicalHistoryController : ControllerBase
    {
        private readonly IMedicalHistoryService _medicalHistoryService;
        private readonly ILogger<MedicalHistoryController> _logger;

        public MedicalHistoryController(IMedicalHistoryService medicalHistoryService, ILogger<MedicalHistoryController> logger)
        {
            _medicalHistoryService = medicalHistoryService;
            _logger = logger;
        }


        // POST: api/medicalhistory
        [HttpPost]
		[Authorize]
		public async Task<IActionResult> SaveMedicalHistory([FromBody] SaveMedicalHistoryCommand command)
        {
            try
            {
                // Try to get the user ID in multiple ways
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }
                
                if (string.IsNullOrEmpty(userId))
                {
                    var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                    return Unauthorized(new { message = "User ID not found in token", claims = allClaims });
                }

                // Validate the command manually first to catch any issues
                var validationContext = new ValidationContext(command);
                var validationResults = new List<ValidationResult>();
                var isValid = Validator.TryValidateObject(command, validationContext, validationResults, true);
                
                if (!isValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = validationResults.Select(vr => new { property = vr.MemberNames.FirstOrDefault() ?? "Unknown", error = vr.ErrorMessage }).ToList()
                    });
                }

                // Check for null values in required nested objects
                if (command.FamilyHistory == null)
                {
                    return BadRequest(new { success = false, message = "Family history cannot be null", errors = new[] { "FamilyHistory is required" } });
                }
                
                if (command.ImmunizationHistory == null)
                {
                    return BadRequest(new { success = false, message = "Immunization history cannot be null", errors = new[] { "ImmunizationHistory is required" } });
                }
                
                if (command.SocialHistory == null)
                {
                    return BadRequest(new { success = false, message = "Social history cannot be null", errors = new[] { "SocialHistory is required" } });
                }

                var result = await _medicalHistoryService.SaveMedicalHistoryAsync(command, userId);
                if (!result.Success)
                {
                    _logger.LogWarning("Failed to save medical history: {Message}, Errors: {@Errors}", 
                        result.Message, result.Errors);
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SaveMedicalHistory");
                return StatusCode(500, new { success = false, message = $"Server error: {ex.Message}" });
            }
        }

        // PUT: api/medicalhistory
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateMedicalHistory([FromBody] SaveMedicalHistoryCommand command)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _medicalHistoryService.UpdateMedicalHistoryAsync(command, userId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // GET: api/MedicalHistory
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMedicalHistory()
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _medicalHistoryService.GetMedicalHistoryByUserIdAsync(userId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        // GET: api/MedicalHistory/qr
        // Generates a NEW QR code (refreshes existing one)
        [HttpGet("qr")]
        [Authorize]
        public async Task<IActionResult> GenerateQrCode()
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _medicalHistoryService.GenerateQrCodeAsync(userId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // GET: api/MedicalHistory/qr/existing
        // Returns the existing stored QR code (or error if none exists/expired)
        [HttpGet("qr/existing")]
        [Authorize]
        public async Task<IActionResult> GetExistingQrCode()
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _medicalHistoryService.GetExistingQrCodeAsync(userId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        // POST: api/MedicalHistory/scan
        [HttpPost("scan")]
        [Authorize] // Requires authentication - user must be logged in
        public async Task<IActionResult> GetMedicalHistoryByQrCode([FromBody] string qrCodeData)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID found in token claims for scan endpoint");
                    return Unauthorized(new { success = false, message = "User ID not found in token" });
                }

                if (string.IsNullOrEmpty(qrCodeData))
                {
                    _logger.LogWarning("Empty QR code data provided by user {UserId}", userId);
                    return BadRequest(new { success = false, message = "QR code data is required" });
                }

                _logger.LogInformation("User {UserId} attempting secure QR code scan", userId);

                // Use the new secure method that can handle both images and encrypted text
                var result = await _medicalHistoryService.GetMedicalHistoryByQrCodeAsync(qrCodeData, userId);
                if (!result.Success)
                {
                    _logger.LogWarning("Secure QR code scan failed for user {UserId}: {Message}", userId, result.Message);
                    
                    // Return 403 for ownership validation failures, 404 for other failures
                    if (result.Message.Contains("not authorized"))
                    {
                        return StatusCode(403, new { success = false, message = result.Message });
                    }
                    return NotFound(new { success = false, message = result.Message, errors = result.Errors });
                }

                _logger.LogInformation("Successfully completed secure QR code scan for user {UserId}", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in secure QR code scan");
                return StatusCode(500, new { success = false, message = "An error occurred while scanning QR code" });
            }
        }

        // GET: api/MedicalHistory/token/{token}
        [HttpGet("token/{token}")]
        [Authorize] // Requires authentication and validates ownership
        public async Task<IActionResult> GetMedicalHistoryByToken(string token)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID found in token claims");
                    return Unauthorized(new { success = false, message = "User ID not found in token" });
                }

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Empty token provided by user {UserId}", userId);
                    return BadRequest(new { success = false, message = "Token is required" });
                }

                _logger.LogInformation("User {UserId} attempting to access token {Token}", userId, token);
        
                // Validate ownership - user can only access their own medical history
                var qrCodeFromToken = $"TOKEN:{token}";
                var validationResult = await _medicalHistoryService.ValidateQrCodeOwnershipAsync(qrCodeFromToken, userId);
                if (!validationResult.Success)
                {
                    _logger.LogWarning("User {UserId} attempted to access token {Token} - Validation failed: {Message}", 
                        userId, token, validationResult.Message);
                    return StatusCode(403, new { success = false, message = validationResult.Message });
                }

                _logger.LogInformation("Ownership validation successful for user {UserId} and token {Token}", userId, token);
                
                var result = await _medicalHistoryService.GetMedicalHistoryByTokenAsync(token);
                if (!result.Success)
                {
                    _logger.LogWarning("Failed to retrieve medical history for token {Token}: {Message}", token, result.Message);
                    return NotFound(new { success = false, message = result.Message, errors = result.Errors });
                }

                _logger.LogInformation("Successfully retrieved medical history for user {UserId} via token {Token}", userId, token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetMedicalHistoryByToken for token {Token}", token);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving medical history" });
            }
        }
        
        // POST: api/MedicalHistory/emergency/scan
        [HttpPost("emergency/scan")]
        [AllowAnonymous] // Emergency endpoint - allows unauthenticated access
        public async Task<IActionResult> EmergencyAccessByQrCode([FromBody] EmergencyAccessRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.QrCodeData))
                return BadRequest("QR code data is required");
                
            // Use default reason if not provided for emergency terminals
            var emergencyReason = string.IsNullOrEmpty(request.EmergencyAccessReason) 
                ? "Emergency Access"
				: request.EmergencyAccessReason;
                
            // This endpoint is specifically for emergency personnel
            // and doesn't require ownership validation
            var result = await _medicalHistoryService.GetEmergencyMedicalHistoryByQrCodeAsync(
                request.QrCodeData, 
                emergencyReason);
                
            if (!result.Success)
                return NotFound(result);
                
            return Ok(result);
        }
        
        // GET: api/MedicalHistory/emergency/token/{token}
        [HttpGet("emergency/token/{token}")]
        [AllowAnonymous] // Emergency endpoint - allows unauthenticated access
        public async Task<IActionResult> EmergencyAccessByToken(string token, [FromQuery] string reason = "Emergency Access")
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token is required");
                
            // Use default reason if not provided for emergency terminals
            var emergencyReason = string.IsNullOrEmpty(reason) 
                ? "Emergency Access"
				: reason;
        
            var result = await _medicalHistoryService.GetEmergencyMedicalHistoryByTokenAsync(token, emergencyReason);
            if (!result.Success)
                return NotFound(result);
                
            return Ok(result);
        }
    }
    
    public class EmergencyAccessRequest
    {
        public string QrCodeData { get; set; }
        public string EmergencyAccessReason ="Emergency Access"; // Default reason
	}
} 