using AutoMapper;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using ILLVentApp.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ILLVentApp.Application.Services
{
    public class EmergencyRequestService : IEmergencyRequestService
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<EmergencyRequestService> _logger;
        private readonly IHospitalService _hospitalService;
        private readonly IMedicalHistoryService _medicalHistoryService;
        private readonly IHubContext<EmergencyHub> _hubContext;
        private readonly IConfiguration _configuration;

        public EmergencyRequestService(
            IAppDbContext context,
            IMapper mapper,
            ILogger<EmergencyRequestService> logger,
            IHospitalService hospitalService,
            IMedicalHistoryService medicalHistoryService,
            IHubContext<EmergencyHub> hubContext,
            IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _hospitalService = hospitalService;
            _medicalHistoryService = medicalHistoryService;
            _hubContext = hubContext;
            _configuration = configuration;
        }

        public async Task<EmergencyRequestResponseDto> CreateEmergencyRequestAsync(string userId, CreateEmergencyRequestDto request)
        {
            try
            {
                _logger.LogInformation("Creating emergency request for user {UserId}", userId);

                // Check if user already has an active emergency request
                var existingRequest = await _context.EmergencyRescueRequests
                    .Where(r => r.UserId == userId && r.RequestStatus == "Pending")
                    .FirstOrDefaultAsync();

                if (existingRequest != null)
                {
                    return new EmergencyRequestResponseDto
                    {
                        Success = false,
                        Message = "You already have an active emergency request",
                        RequestId = existingRequest.RequestId
                    };
                }

                // Get medical history if provided (using your existing anonymous endpoints)
                var medicalHistoryData = await GetMedicalHistoryForEmergencyAsync(request.MedicalHistoryQrCode, request.MedicalHistoryToken);

                // AUTOMATIC HOSPITAL SELECTION FROM CONFIGURATION
                var targetHospitalIds = await GetTargetHospitalsAsync();
                
                if (!targetHospitalIds.Any())
                {
                    return new EmergencyRequestResponseDto
                    {
                        Success = false,
                        Message = "No target hospitals configured. Please contact system administrator."
                    };
                }

                _logger.LogInformation("Automatically targeting {Count} hospitals: {HospitalIds}", 
                    targetHospitalIds.Count, string.Join(", ", targetHospitalIds));

                // Get the specified hospitals
                var selectedHospitals = await _context.Hospitals
                    .Where(h => targetHospitalIds.Contains(h.HospitalId) && h.IsAvailable)
                    .ToListAsync();

                if (!selectedHospitals.Any())
                {
                    return new EmergencyRequestResponseDto
                    {
                        Success = false,
                        Message = "None of the configured hospitals are currently available. Please call emergency services directly."
                    };
                }

                // Log which hospitals were found vs configured
                var foundIds = selectedHospitals.Select(h => h.HospitalId).ToList();
                var notFoundIds = targetHospitalIds.Except(foundIds).ToList();
                
                if (notFoundIds.Any())
                {
                    _logger.LogWarning("Some configured hospitals not available: {NotFoundIds}", string.Join(", ", notFoundIds));
                }

                var targetHospitals = selectedHospitals.Select(h => _mapper.Map<HospitalDto>(h)).ToList();

                // Create the emergency request
                var emergencyRequest = new EmergencyRescueRequest
                {
                    UserId = userId,
                    RequestDescription = "Emergency rescue request",
                    RequestImage = string.Empty,
                    RequestStatus = "Pending",
                    InjuryDescription = request.InjuryDescription ?? "Emergency assistance needed",
                    InjuryPhotoUrl = string.Empty,
                    Timestamp = DateTime.Now, // Use local time instead of UTC for proper display
                    // Set real location coordinates (Cairo, Egypt) instead of 0,0
                    UserLatitude = request.UserLatitude ?? 30.0618, // Cairo latitude
                    UserLongitude = request.UserLongitude ?? 31.2186, // Cairo longitude
                    LocationTimestamp = DateTime.Now, // Use local time for consistency
                    MedicalHistorySnapshot = medicalHistoryData?.Json ?? string.Empty,
                    MedicalHistorySource = medicalHistoryData?.Source ?? "None",
                    NotifiedHospitalIds = JsonSerializer.Serialize(targetHospitals.Select(h => h.HospitalId).ToList()),
                    RejectedByHospitalIds = string.Empty, // Initialize as empty - no rejections yet
                    RequestPriority = request.Priority ?? 1
                };

                // Handle injury photo if provided
                if (!string.IsNullOrEmpty(request.InjuryPhotoBase64))
                {
                    // TODO: Upload to Azure Blob Storage and encrypt URL
                    // For now, we'll store a placeholder indicating photo was provided
                    emergencyRequest.InjuryPhotoUrl = "PHOTO_PROVIDED";
                    emergencyRequest.RequestImage = "PHOTO_PROVIDED";
                }

                _context.EmergencyRescueRequests.Add(emergencyRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Emergency request {RequestId} created successfully and sent to {Count} hospitals", 
                    emergencyRequest.RequestId, targetHospitals.Count);

                // Notify hospitals via SignalR
                await NotifyHospitalsAsync(emergencyRequest, targetHospitals);

                // Prepare response
                var hospitalDtos = targetHospitals.Select(h => new NearbyHospitalDto
                {
                    HospitalId = h.HospitalId,
                    Name = h.Name,
                    Location = h.Location,
                    DistanceKm = 0.0,
                    IsAvailable = h.IsAvailable,
                    ContactNumber = h.ContactNumber,
                    Specialties = h.Specialties
                }).ToList();
                
                return new EmergencyRequestResponseDto
                {
                    Success = true,
                    Message = $"Emergency request sent to {targetHospitals.Count} hospital(s) automatically.",
                    RequestId = emergencyRequest.RequestId,
                    NearbyHospitals = hospitalDtos,
                    TrackingId = $"EMR_{emergencyRequest.RequestId}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating emergency request for user {UserId}", userId);
                return new EmergencyRequestResponseDto
                {
                    Success = false,
                    Message = "An error occurred while creating your emergency request. Please try again."
                };
            }
        }

        public async Task<EmergencyRequestDetailsDto> GetEmergencyRequestDetailsAsync(int requestId, int hospitalId)
        {
            try
            {
                var request = await _context.EmergencyRescueRequests
                    .Include(r => r.User)
                    .Where(r => r.RequestId == requestId && r.RequestStatus == "Pending")
                    .FirstOrDefaultAsync();

                if (request == null)
                    return null;

                // Get hospital location for distance calculation
                var hospital = await _context.Hospitals.FindAsync(hospitalId);
                if (hospital == null)
                    return null;

                var distance = CalculateDistance(
                    request.UserLatitude, request.UserLongitude,
                    hospital.Latitude, hospital.Longitude);

                // Parse medical history
                EmergencyMedicalHistoryDto medicalHistory = null;
                if (!string.IsNullOrEmpty(request.MedicalHistorySnapshot))
                {
                    try
                    {
                        _logger.LogInformation("Raw medical history snapshot for request {RequestId}: {Snapshot}", 
                            requestId, request.MedicalHistorySnapshot);
                            
                        var historyData = JsonSerializer.Deserialize<EmergencyMedicalHistoryDto>(request.MedicalHistorySnapshot);
                        historyData.DataSource = request.MedicalHistorySource;
                        
                        _logger.LogInformation("Parsed medical history for request {RequestId}: Age={Age}, Weight={Weight}, Height={Height}, Gender={Gender}", 
                            requestId, historyData.Age, historyData.Weight, historyData.Height, historyData.Gender);
                            
                        medicalHistory = historyData;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing medical history for request {RequestId}", requestId);
                    }
                }
                else
                {
                    _logger.LogWarning("No medical history snapshot found for request {RequestId}", requestId);
                }

                return new EmergencyRequestDetailsDto
                {
                    RequestId = request.RequestId,
                    PatientName = $"{request.User.FirstName} {request.User.Surname}",
                    InjuryDescription = request.InjuryDescription,
                    InjuryPhotoUrl = request.InjuryPhotoUrl,
                    UserLatitude = request.UserLatitude,
                    UserLongitude = request.UserLongitude,
                    DistanceFromHospital = Math.Round(distance, 2),
                    Timestamp = request.Timestamp,
                    Priority = request.RequestPriority,
                    MedicalHistory = medicalHistory
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency request details for request {RequestId}", requestId);
                return null;
            }
        }

        public async Task<bool> RespondToEmergencyRequestAsync(HospitalEmergencyResponseDto response)
        {
            try
            {
                var request = await _context.EmergencyRescueRequests
                    .Where(r => r.RequestId == response.RequestId && r.RequestStatus == "Pending")
                    .FirstOrDefaultAsync();

                if (request == null)
                {
                    _logger.LogWarning("Emergency request {RequestId} not found or not pending", response.RequestId);
                    return false;
                }

                if (response.IsAccepted)
                {
                    // Accept the request
                    request.RequestStatus = "Accepted";
                    request.AcceptedHospitalId = response.HospitalId;
                    request.HospitalResponseTime = DateTime.Now; // Use local time for consistency

                    // Get hospital details
                    var hospital = await _context.Hospitals.FindAsync(response.HospitalId);

                    var hospitalAcceptedData = new
                    {
                        RequestId = request.RequestId,
                        Hospital = new
                        {
                            hospital.HospitalId,
                            hospital.Name,
                            hospital.Location,
                            hospital.ContactNumber,
                            hospital.Latitude,
                            hospital.Longitude
                        },
                        EstimatedResponseTime = response.EstimatedResponseTimeMinutes ?? 15, // Default to 15 minutes if not provided
                        AmbulanceAvailable = response.AmbulanceAvailable,
                        Message = response.ResponseMessage
                    };

                    // Notify user via SignalR (existing notification)
                    await _hubContext.Clients.Group($"User_{request.UserId}")
                        .SendAsync("HospitalAccepted", hospitalAcceptedData);

                    // Notify request-specific group (for mobile app real-time updates)
                    await _hubContext.Clients.Group($"Emergency_{request.RequestId}")
                        .SendAsync("HospitalAccepted", hospitalAcceptedData);

                    _logger.LogInformation("Emergency request {RequestId} accepted by hospital {HospitalId}", 
                        response.RequestId, response.HospitalId);
                }
                else
                {
                    // Hospital rejected - track the rejection and remove from this hospital's view
                    List<int> rejectedIds;
                    try
                    {
                        rejectedIds = string.IsNullOrEmpty(request.RejectedByHospitalIds) 
                            ? new List<int>() 
                            : JsonSerializer.Deserialize<List<int>>(request.RejectedByHospitalIds);
                    }
                    catch
                    {
                        rejectedIds = new List<int>();
                    }

                    // Add this hospital to rejected list if not already there
                    if (!rejectedIds.Contains(response.HospitalId))
                    {
                        rejectedIds.Add(response.HospitalId);
                        request.RejectedByHospitalIds = JsonSerializer.Serialize(rejectedIds);
                    }

                    // Notify this specific hospital that the request was rejected (for real-time removal)
                    await _hubContext.Clients.Group($"Hospital_{response.HospitalId}")
                        .SendAsync("EmergencyRequestRejected", new
                        {
                            RequestId = request.RequestId,
                            Message = "Request rejected and removed from your dashboard"
                        });

                    _logger.LogInformation("Emergency request {RequestId} rejected by hospital {HospitalId}: {Message}", 
                        response.RequestId, response.HospitalId, response.ResponseMessage);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to emergency request {RequestId}", response.RequestId);
                return false;
            }
        }

        public async Task<bool> UpdateEmergencyLocationAsync(LocationUpdateDto locationUpdate)
        {
            try
            {
                var request = await _context.EmergencyRescueRequests
                    .Where(r => r.RequestId == locationUpdate.RequestId && 
                               (r.RequestStatus == "Pending" || r.RequestStatus == "Accepted"))
                    .FirstOrDefaultAsync();

                if (request == null)
                    return false;

                // Update location
                request.UserLatitude = locationUpdate.Latitude;
                request.UserLongitude = locationUpdate.Longitude;
                request.LocationTimestamp = locationUpdate.Timestamp;

                await _context.SaveChangesAsync();

                // Notify tracking parties via SignalR
                await _hubContext.Clients.Group($"Emergency_{request.RequestId}")
                    .SendAsync("LocationUpdated", new
                    {
                        RequestId = request.RequestId,
                        Latitude = locationUpdate.Latitude,
                        Longitude = locationUpdate.Longitude,
                        Timestamp = locationUpdate.Timestamp
                    });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location for emergency request {RequestId}", locationUpdate.RequestId);
                return false;
            }
        }

        public async Task<List<EmergencyRequestDetailsDto>> GetActiveEmergencyRequestsForHospitalAsync(int hospitalId, double radiusKm = 20.0)
        {
            try
            {
                var hospital = await _context.Hospitals.FindAsync(hospitalId);
                if (hospital == null)
                {
                    _logger.LogWarning("Hospital {HospitalId} not found", hospitalId);
                    return new List<EmergencyRequestDetailsDto>();
                }

                var activeRequests = await _context.EmergencyRescueRequests
                    .Include(r => r.User)
                    .Where(r => r.RequestStatus == "Pending")
                    .ToListAsync();

                // Calculate distances and filter by radius, notification list, and rejection status
                var nearbyRequests = activeRequests
                    .Where(r => 
                    {
                        // Check if this hospital has rejected this request
                        try
                        {
                            if (!string.IsNullOrEmpty(r.RejectedByHospitalIds))
                            {
                                var rejectedIds = JsonSerializer.Deserialize<List<int>>(r.RejectedByHospitalIds);
                                if (rejectedIds.Contains(hospitalId))
                                {
                                    return false; // This hospital rejected this request, don't show it
                                }
                            }
                        }
                        catch
                        {
                            // If parsing fails, continue with other filters
                        }

                        // Always calculate distance since we're using real coordinates
                        var distance = CalculateDistance(r.UserLatitude, r.UserLongitude, hospital.Latitude, hospital.Longitude);
                        
                        // First check if within radius
                        if (distance <= radiusKm)
                            return true;
                            
                        // Also check if this hospital was specifically notified (for targeted approach)
                        try
                        {
                            if (!string.IsNullOrEmpty(r.NotifiedHospitalIds))
                            {
                                var notifiedIds = JsonSerializer.Deserialize<List<int>>(r.NotifiedHospitalIds);
                                if (notifiedIds.Contains(hospitalId))
                                {
                                    return true;
                                }
                            }
                        }
                        catch
                        {
                            // If parsing fails, rely on distance only
                        }
                        
                        return false;
                    })
                    .Select(r => new EmergencyRequestDetailsDto
                    {
                        RequestId = r.RequestId,
                        PatientName = $"{r.User.FirstName} {r.User.Surname}",
                        InjuryDescription = r.InjuryDescription,
                        InjuryPhotoUrl = r.InjuryPhotoUrl,
                        UserLatitude = r.UserLatitude,
                        UserLongitude = r.UserLongitude,
                        DistanceFromHospital = Math.Round(CalculateDistance(r.UserLatitude, r.UserLongitude, hospital.Latitude, hospital.Longitude), 2),
                        Timestamp = r.Timestamp,
                        Priority = r.RequestPriority,
                        MedicalHistory = ParseMedicalHistory(r.MedicalHistorySnapshot, r.MedicalHistorySource)
                    })
                    .OrderBy(r => r.Priority)
                    .ThenBy(r => r.DistanceFromHospital)
                    .ToList();

                _logger.LogInformation("Hospital {HospitalId} has {Count} active emergency requests within {Radius}km", 
                    hospitalId, nearbyRequests.Count, radiusKm);

                return nearbyRequests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active emergency requests for hospital {HospitalId}", hospitalId);
                return new List<EmergencyRequestDetailsDto>();
            }
        }

        public async Task<bool> CompleteEmergencyRequestAsync(int requestId, string userId)
        {
            try
            {
                var request = await _context.EmergencyRescueRequests
                    .Where(r => r.RequestId == requestId && r.UserId == userId)
                    .FirstOrDefaultAsync();

                if (request == null)
                    return false;

                request.RequestStatus = "Completed";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Emergency request {RequestId} completed by user {UserId}", requestId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing emergency request {RequestId}", requestId);
                return false;
            }
        }

        public async Task<EmergencyRequestDetailsDto> GetUserActiveEmergencyRequestAsync(string userId)
        {
            try
            {
                var request = await _context.EmergencyRescueRequests
                    .Include(r => r.AcceptedHospital)
                    .Where(r => r.UserId == userId && (r.RequestStatus == "Pending" || r.RequestStatus == "Accepted"))
                    .FirstOrDefaultAsync();

                if (request == null)
                    return null;

                return new EmergencyRequestDetailsDto
                {
                    RequestId = request.RequestId,
                    PatientName = "You",
                    InjuryDescription = request.InjuryDescription,
                    InjuryPhotoUrl = request.InjuryPhotoUrl,
                    UserLatitude = request.UserLatitude,
                    UserLongitude = request.UserLongitude,
                    Timestamp = request.Timestamp,
                    Priority = request.RequestPriority,
                    MedicalHistory = ParseMedicalHistory(request.MedicalHistorySnapshot, request.MedicalHistorySource)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active emergency request for user {UserId}", userId);
                return null;
            }
        }

        // Private helper methods
        private async Task<(string Json, string Source)?> GetMedicalHistoryForEmergencyAsync(string qrCode, string token)
        {
            try
            {
                _logger.LogInformation("GetMedicalHistoryForEmergencyAsync called with QrCode: {QrCode}, Token: {Token}", 
                    string.IsNullOrEmpty(qrCode) ? "NULL" : "PROVIDED", 
                    string.IsNullOrEmpty(token) ? "NULL" : token);

                // Use your existing anonymous endpoints for medical history
                if (!string.IsNullOrEmpty(qrCode))
                {
                    _logger.LogInformation("Attempting to get medical history via QR code");
                    var qrResult = await _medicalHistoryService.GetEmergencyMedicalHistoryByQrCodeAsync(qrCode, "Emergency Request");
                    if (qrResult.Success)
                    {
                        _logger.LogInformation("Successfully retrieved medical history via QR code: Age={Age}, Weight={Weight}, Height={Height}, Gender={Gender}", 
                            qrResult.Data.Age, qrResult.Data.Weight, qrResult.Data.Height, qrResult.Data.Gender);

                        var historyDto = new EmergencyMedicalHistoryDto
                        {
                            // Basic patient information
                            Age = qrResult.Data.Age,
                            Weight = qrResult.Data.Weight,
                            Height = qrResult.Data.Height,
                            Gender = qrResult.Data.Gender,
                            
                            // Blood and medical conditions
                            BloodType = qrResult.Data.BloodType,
                            HasAllergies = qrResult.Data.HasAllergies,
                            AllergiesDetails = qrResult.Data.AllergiesDetails,
                            MedicalConditions = qrResult.Data.MedicalConditions?.Select(mc => mc.Condition).ToList() ?? new List<string>(),
                            HasBloodTransfusionObjection = qrResult.Data.HasBloodTransfusionObjection,
                            HasDiabetes = qrResult.Data.HasDiabetes,
                            DiabetesType = qrResult.Data.DiabetesType,
                            HasHighBloodPressure = qrResult.Data.HasHighBloodPressure,
                            HasLowBloodPressure = qrResult.Data.HasLowBloodPressure,
                            
                            // Surgical history
                            HasSurgeryHistory = qrResult.Data.HasSurgeryHistory,
                            SurgicalHistories = qrResult.Data.SurgicalHistories?.Select(sh => new EmergencySurgicalHistoryDto
                            {
                                SurgeryType = sh.SurgeryType,
                                Date = sh.Date,
                                Details = sh.Details
                            }).ToList() ?? new List<EmergencySurgicalHistoryDto>()
                        };
                        var json = JsonSerializer.Serialize(historyDto);
                        _logger.LogInformation("Created medical history JSON for QR code (length: {Length})", json.Length);
                        return (json, "QRCode");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve medical history via QR code: {Message}", qrResult.Message);
                    }
                }

                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("Attempting to get medical history via token: {Token}", token);
                    var tokenResult = await _medicalHistoryService.GetEmergencyMedicalHistoryByTokenAsync(token, "Emergency Request");
                    if (tokenResult.Success)
                    {
                        _logger.LogInformation("Successfully retrieved medical history via token: Age={Age}, Weight={Weight}, Height={Height}, Gender={Gender}", 
                            tokenResult.Data.Age, tokenResult.Data.Weight, tokenResult.Data.Height, tokenResult.Data.Gender);

                        var historyDto = new EmergencyMedicalHistoryDto
                        {
                            // Basic patient information
                            Age = tokenResult.Data.Age,
                            Weight = tokenResult.Data.Weight,
                            Height = tokenResult.Data.Height,
                            Gender = tokenResult.Data.Gender,
                            
                            // Blood and medical conditions
                            BloodType = tokenResult.Data.BloodType,
                            HasAllergies = tokenResult.Data.HasAllergies,
                            AllergiesDetails = tokenResult.Data.AllergiesDetails,
                            MedicalConditions = tokenResult.Data.MedicalConditions?.Select(mc => mc.Condition).ToList() ?? new List<string>(),
                            HasBloodTransfusionObjection = tokenResult.Data.HasBloodTransfusionObjection,
                            HasDiabetes = tokenResult.Data.HasDiabetes,
                            DiabetesType = tokenResult.Data.DiabetesType,
                            HasHighBloodPressure = tokenResult.Data.HasHighBloodPressure,
                            HasLowBloodPressure = tokenResult.Data.HasLowBloodPressure,
                            
                            // Surgical history
                            HasSurgeryHistory = tokenResult.Data.HasSurgeryHistory,
                            SurgicalHistories = tokenResult.Data.SurgicalHistories?.Select(sh => new EmergencySurgicalHistoryDto
                            {
                                SurgeryType = sh.SurgeryType,
                                Date = sh.Date,
                                Details = sh.Details
                            }).ToList() ?? new List<EmergencySurgicalHistoryDto>()
                        };
                        var json = JsonSerializer.Serialize(historyDto);
                        _logger.LogInformation("Created medical history JSON for token (length: {Length})", json.Length);
                        return (json, "Token");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve medical history via token: {Message}", tokenResult.Message);
                    }
                }

                _logger.LogWarning("No medical history retrieved - both QR code and token were null/empty or failed");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medical history for emergency");
                return null;
            }
        }

        private async Task NotifyHospitalsAsync(EmergencyRescueRequest request, List<HospitalDto> hospitals)
        {
            try
            {
                foreach (var hospital in hospitals)
                {
                    await _hubContext.Clients.Group($"Hospital_{hospital.HospitalId}")
                        .SendAsync("NewEmergencyRequest", new
                        {
                            RequestId = request.RequestId,
                            PatientLocation = new { request.UserLatitude, request.UserLongitude },
                            InjuryDescription = request.InjuryDescription,
                            Priority = request.RequestPriority,
                            Timestamp = request.Timestamp,
                            Distance = Math.Round(CalculateDistance(
                                request.UserLatitude, request.UserLongitude,
                                hospital.Latitude, hospital.Longitude), 2),
                            HasMedicalHistory = !string.IsNullOrEmpty(request.MedicalHistorySnapshot)
                        });
                }

                _logger.LogInformation("Notified {Count} hospitals about emergency request {RequestId}", 
                    hospitals.Count, request.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying hospitals about emergency request {RequestId}", request.RequestId);
            }
        }

        private EmergencyMedicalHistoryDto ParseMedicalHistory(string json, string source)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                var history = JsonSerializer.Deserialize<EmergencyMedicalHistoryDto>(json);
                history.DataSource = source;
                return history;
            }
            catch
            {
                return null;
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula to calculate distance between two points on Earth
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

        private async Task<List<int>> GetTargetHospitalsAsync()
        {
            try
            {
                // Option 1: Get from configuration (if specified)
                var configuredHospitalIds = _configuration.GetSection("Emergency:DefaultHospitalIds").Get<List<int>>();
                
                if (configuredHospitalIds != null && configuredHospitalIds.Any())
                {
                    _logger.LogInformation("Using configured hospital IDs: {HospitalIds}", string.Join(", ", configuredHospitalIds));
                    return configuredHospitalIds;
                }

                // Option 2: Fallback to single hospital from config
                var singleHospitalId = _configuration.GetValue<int?>("Emergency:DefaultHospitalId");
                if (singleHospitalId.HasValue && singleHospitalId.Value > 0)
                {
                    _logger.LogInformation("Using single configured hospital ID: {HospitalId}", singleHospitalId.Value);
                    return new List<int> { singleHospitalId.Value };
                }

                // Option 3: Smart selection - get first 2 available hospitals
                var availableHospitals = await _context.Hospitals
                    .Where(h => h.IsAvailable)
                    .OrderBy(h => h.HospitalId) // You can change this to any ordering logic
                    .Take(2) // Take first 2 available hospitals
                    .Select(h => h.HospitalId)
                    .ToListAsync();

                if (availableHospitals.Any())
                {
                    _logger.LogInformation("Auto-selected available hospitals: {HospitalIds}", string.Join(", ", availableHospitals));
                    return availableHospitals;
                }

                // Option 4: Last resort - log warning and return empty
                _logger.LogWarning("No hospitals available for emergency requests");
                return new List<int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting target hospitals");
                return new List<int>();
            }
        }
    }
} 