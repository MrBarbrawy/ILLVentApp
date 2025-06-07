using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
namespace ILLVentApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmergencyController : ControllerBase
    {
        private readonly IEmergencyRequestService _emergencyService;
        private readonly ILogger<EmergencyController> _logger;
        private readonly IAppDbContext _context;
        private readonly IConfiguration _configuration;

        public EmergencyController(
            IEmergencyRequestService emergencyService,
            ILogger<EmergencyController> logger,
            IAppDbContext context,
            IConfiguration configuration)
        {
            _emergencyService = emergencyService;
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Emergency/request
        /// <summary>
        /// Create a new emergency request (User authenticated)
        /// Automatically sends to configured hospitals - no hospital IDs needed!
        /// </summary>
        [HttpPost("request")]
        [Authorize]
        public async Task<IActionResult> CreateEmergencyRequest([FromBody] CreateEmergencyRequestDto request)
        {
            try
            {
                // Manual validation instead of ModelState since ModelState is incorrectly requiring optional fields
                if (request == null)
                    return BadRequest(new { success = false, message = "Request body is required" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found");

                _logger.LogInformation("Emergency request initiated by user {UserId} - automatic hospital selection", userId);

                var result = await _emergencyService.CreateEmergencyRequestAsync(userId, request);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                _logger.LogInformation("Emergency request {RequestId} created successfully for user {UserId}", 
                    result.RequestId, userId);

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        requestId = result.RequestId,
                        trackingId = result.TrackingId,
                        targetHospitals = result.NearbyHospitals,
                        hospitalsNotified = result.NearbyHospitals.Count,
                        selectionMethod = "Automatic"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating emergency request");
                return StatusCode(500, new { success = false, message = "An error occurred while creating your emergency request" });
            }
        }

        // PUT: api/Emergency/location
        /// <summary>
        /// Update user location during active emergency (Optional - for future geographic features)
        /// </summary>
        [HttpPut("location")]
        [Authorize]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto locationUpdate)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _emergencyService.UpdateEmergencyLocationAsync(locationUpdate);

                if (!result)
                    return NotFound(new { success = false, message = "Emergency request not found or not active" });

                return Ok(new { success = true, message = "Location updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating emergency location for request {RequestId}", locationUpdate.RequestId);
                return StatusCode(500, new { success = false, message = "Error updating location" });
            }
        }

        // GET: api/Emergency/my-request
        /// <summary>
        /// Get user's active emergency request status
        /// </summary>
        [HttpGet("my-request")]
        [Authorize]
        public async Task<IActionResult> GetMyActiveEmergencyRequest()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var request = await _emergencyService.GetUserActiveEmergencyRequestAsync(userId);

                if (request == null)
                    return Ok(new { success = true, hasActiveRequest = false });

                return Ok(new
                {
                    success = true,
                    hasActiveRequest = true,
                    data = request
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user active emergency request");
                return StatusCode(500, new { success = false, message = "Error retrieving emergency request" });
            }
        }

        // POST: api/Emergency/complete
        /// <summary>
        /// Complete/close an emergency request (User confirms emergency is resolved)
        /// </summary>
        [HttpPost("complete")]
        [Authorize]
        public async Task<IActionResult> CompleteEmergencyRequest([FromBody] CompleteEmergencyRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _emergencyService.CompleteEmergencyRequestAsync(request.RequestId, userId);

                if (!result)
                    return NotFound(new { success = false, message = "Emergency request not found" });

                return Ok(new { success = true, message = "Emergency request completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing emergency request {RequestId}", request.RequestId);
                return StatusCode(500, new { success = false, message = "Error completing emergency request" });
            }
        }

        // ================== HOSPITAL ENDPOINTS ==================

        // GET: api/Emergency/hospital/requests
        /// <summary>
        /// Get active emergency requests for a hospital (Hospital Dashboard)
        /// </summary>
        [HttpGet("hospital/requests")]
        [Authorize] // Hospital staff authenticated
        public async Task<IActionResult> GetActiveEmergencyRequestsForHospital([FromQuery] int hospitalId, [FromQuery] double radiusKm = 20.0)
        {
            try
            {
                if (hospitalId <= 0)
                    return BadRequest(new { success = false, message = "Valid hospital ID is required" });

                var requests = await _emergencyService.GetActiveEmergencyRequestsForHospitalAsync(hospitalId, radiusKm);

                return Ok(new
                {
                    success = true,
                    data = requests,
                    count = requests.Count,
                    radiusKm = radiusKm
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency requests for hospital {HospitalId}", hospitalId);
                return StatusCode(500, new { success = false, message = "Error retrieving emergency requests" });
            }
        }

        // GET: api/Emergency/hospital/request/{requestId}
        /// <summary>
        /// Get detailed emergency request information for hospital review
        /// </summary>
        [HttpGet("hospital/request/{requestId}")]
        [Authorize] // Hospital staff authenticated
        public async Task<IActionResult> GetEmergencyRequestDetails(int requestId, [FromQuery] int hospitalId)
        {
            try
            {
                if (hospitalId <= 0)
                    return BadRequest(new { success = false, message = "Valid hospital ID is required" });

                var request = await _emergencyService.GetEmergencyRequestDetailsAsync(requestId, hospitalId);

                if (request == null)
                    return NotFound(new { success = false, message = "Emergency request not found" });

                return Ok(new
                {
                    success = true,
                    data = request
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency request details for request {RequestId}", requestId);
                return StatusCode(500, new { success = false, message = "Error retrieving request details" });
            }
        }

        // POST: api/Emergency/hospital/respond
        /// <summary>
        /// Hospital responds to emergency request (accept/reject)
        /// </summary>
        [HttpPost("hospital/respond")]
        [Authorize]
        public async Task<IActionResult> RespondToEmergencyRequest([FromBody] HospitalEmergencyResponseDto response)
        {
            try
            {
                // Custom validation for acceptance
                if (response.IsAccepted && (!response.EstimatedResponseTimeMinutes.HasValue || response.EstimatedResponseTimeMinutes.Value <= 0))
                {
                    return BadRequest(new { success = false, message = "Estimated response time is required when accepting an emergency request" });
                }

                // If rejecting, clear unnecessary fields
                if (!response.IsAccepted)
                {
                    response.EstimatedResponseTimeMinutes = null;
                    response.AmbulanceAvailable = false;
                }

                var result = await _emergencyService.RespondToEmergencyRequestAsync(response);

                if (result)
                {
                    var action = response.IsAccepted ? "accepted" : "rejected";
                    return Ok(new { success = true, message = $"Emergency request {action} successfully" });
                }

                return BadRequest(new { success = false, message = "Failed to respond to emergency request" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to emergency request");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ================== SYSTEM ENDPOINTS ==================

        // GET: api/Emergency/health
        /// <summary>
        /// Health check endpoint for emergency service
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                service = "Emergency Request Service",
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        // GET: api/Emergency/info
        /// <summary>
        /// Get information about available emergency endpoints
        /// </summary>
        [HttpGet("info")]
        [AllowAnonymous]
        public IActionResult GetEmergencyInfo()
        {
            return Ok(new
            {
                service = "Emergency Request Service",
                approach = "Automated Hospital Selection",
                availableEndpoints = new
                {
                    createRequest = new
                    {
                        method = "POST",
                        endpoint = "/api/Emergency/request",
                        description = "Create emergency request - hospitals selected automatically",
                        requiresAuth = true,
                        example = new { injuryDescription = "Chest pain", priority = 1 }
                    },
                    quickRequest = new
                    {
                        method = "POST",
                        endpoint = "/api/Emergency/quick-request",
                        description = "Quick emergency request (same as main request)",
                        requiresAuth = true,
                        example = new { injuryDescription = "Emergency help needed", priority = 1 }
                    },
                    availableHospitals = new
                    {
                        method = "GET",
                        endpoint = "/api/Emergency/available-hospitals",
                        description = "Get list of available hospitals (for admin reference)",
                        requiresAuth = true
                    }
                },
                configuration = new
                {
                    hospitalSelection = "Automatic - configured in appsettings.json",
                    defaultHospitals = "Emergency:DefaultHospitalIds",
                    fallbackLogic = "Auto-selects first 2 available hospitals if not configured",
                    testMode = true
                }
            });
        }

        // GET: api/Emergency/available-hospitals
        /// <summary>
        /// Get list of available hospitals (for admin reference)
        /// </summary>
        [HttpGet("available-hospitals")]
        [Authorize]
        public async Task<IActionResult> GetAvailableHospitals()
        {
            try
            {
                var hospitals = await _context.Hospitals
                    .Where(h => h.IsAvailable)
                    .Select(h => new
                    {
                        hospitalId = h.HospitalId,
                        name = h.Name,
                        location = h.Location,
                        contactNumber = h.ContactNumber,
                        specialties = h.Specialties
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = hospitals,
                    count = hospitals.Count,
                    message = "Available hospitals (for admin reference - selection is automatic)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available hospitals");
                return StatusCode(500, new { success = false, message = "Error retrieving available hospitals" });
            }
        }

        // POST: api/Emergency/quick-request
        /// <summary>
        /// Quick emergency request (same as main request - hospitals selected automatically)
        /// </summary>
        [HttpPost("quick-request")]
        [Authorize]
        public async Task<IActionResult> CreateQuickEmergencyRequest([FromBody] QuickEmergencyRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found");

                var emergencyRequest = new CreateEmergencyRequestDto
                {
                    InjuryDescription = request.InjuryDescription ?? "Emergency assistance needed",
                    Priority = request.Priority,
                    MedicalHistoryQrCode = request.MedicalHistoryQrCode,
                    MedicalHistoryToken = request.MedicalHistoryToken
                };

                _logger.LogInformation("Quick emergency request initiated by user {UserId}", userId);

                var result = await _emergencyService.CreateEmergencyRequestAsync(userId, emergencyRequest);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new
                {
                    success = true,
                    message = "Emergency request created successfully!",
                    data = new
                    {
                        requestId = result.RequestId,
                        trackingId = result.TrackingId,
                        targetHospitals = result.NearbyHospitals,
                        hospitalsNotified = result.NearbyHospitals.Count,
                        selectionMethod = "Automatic"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quick emergency request");
                return StatusCode(500, new { success = false, message = "Error creating quick emergency request" });
            }
        }

        // GET: api/Emergency/status/{requestId}
        /// <summary>
        /// Check emergency request status (for loading screen updates)
        /// </summary>
        [HttpGet("status/{requestId}")]
        [Authorize]
        public async Task<IActionResult> GetEmergencyRequestStatus(int requestId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var request = await _context.EmergencyRescueRequests
                    .Where(r => r.RequestId == requestId && r.UserId == userId)
                    .Select(r => new
                    {
                        RequestId = r.RequestId,
                        Status = r.RequestStatus,
                        CreatedAt = r.Timestamp,
                        AcceptedHospitalId = r.AcceptedHospitalId,
                        HospitalResponseTime = r.HospitalResponseTime,
                        UserLatitude = r.UserLatitude,
                        UserLongitude = r.UserLongitude
                    })
                    .FirstOrDefaultAsync();

                if (request == null)
                    return NotFound(new { success = false, message = "Emergency request not found" });

                // Base response object
                var baseResponse = new
                {
                    requestId = request.RequestId,
                    status = request.Status,
                    createdAt = request.CreatedAt,
                    waitingTime = DateTime.Now.Subtract(request.CreatedAt).TotalMinutes,
                    hasHospitalResponse = request.AcceptedHospitalId.HasValue,
                    hospitalResponseTime = request.HospitalResponseTime,
                    location = new
                    {
                        latitude = request.UserLatitude,
                        longitude = request.UserLongitude
                    }
                };

                // If hospital has accepted, include hospital details
                if (request.AcceptedHospitalId.HasValue)
                {
                    try
                    {
                        var hospital = await _context.Hospitals
                            .Where(h => h.HospitalId == request.AcceptedHospitalId.Value)
                            .Select(h => new
                            {
                                hospitalId = h.HospitalId,
                                name = h.Name,
                                location = h.Location,
                                contactNumber = h.ContactNumber,
                                latitude = h.Latitude,
                                longitude = h.Longitude
                            })
                            .FirstOrDefaultAsync();

                        if (hospital != null)
                        {
                            // Calculate distance with null checks
                            double distanceKm = 0.0;
                            try
                            {
                                if (request.UserLatitude != 0 && request.UserLongitude != 0 && 
                                    hospital.latitude != 0 && hospital.longitude != 0)
                                {
                                    distanceKm = Math.Round(CalculateDistance(
                                        request.UserLatitude, request.UserLongitude,
                                        hospital.latitude, hospital.longitude), 2);
                                }
                            }
                            catch (Exception distanceEx)
                            {
                                _logger.LogWarning(distanceEx, "Error calculating distance for request {RequestId}", requestId);
                                distanceKm = 0.0; // Default to 0 if calculation fails
                            }

                            return Ok(new
                            {
                                success = true,
                                data = new
                                {
                                    requestId = request.RequestId,
                                    status = request.Status,
                                    createdAt = request.CreatedAt,
                                    waitingTime = DateTime.Now.Subtract(request.CreatedAt).TotalMinutes,
                                    hasHospitalResponse = true,
                                    hospitalResponseTime = request.HospitalResponseTime,
                                    acceptedHospital = new
                                    {
                                        hospitalId = hospital.hospitalId,
                                        name = hospital.name,
                                        location = hospital.location,
                                        contactNumber = hospital.contactNumber,
                                        latitude = hospital.latitude,
                                        longitude = hospital.longitude,
                                        distanceKm = distanceKm
                                    },
                                    location = new
                                    {
                                        latitude = request.UserLatitude,
                                        longitude = request.UserLongitude
                                    }
                                }
                            });
                        }
                    }
                    catch (Exception hospitalEx)
                    {
                        _logger.LogError(hospitalEx, "Error getting hospital details for request {RequestId}", requestId);
                        // Continue with base response if hospital details fail
                    }
                }

                return Ok(new
                {
                    success = true,
                    data = baseResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency request status for {RequestId}", requestId);
                return StatusCode(500, new { success = false, message = "Error retrieving request status" });
            }
        }

        // Helper method for distance calculation (same as service)
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
} 