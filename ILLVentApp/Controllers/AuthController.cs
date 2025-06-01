using AutoMapper;
using ILLVentApp.Application.DTOs;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace ILLVentApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Produces("application/json")]
	public class AuthController : ControllerBase
	{
		private readonly IAuthService _authService;
		private readonly IMapper _mapper;
		private readonly ILogger<AuthController> _logger;
		private readonly IOtpService _otpService;
		private readonly UserManager<User> _userManager;

		public AuthController(
			IAuthService authService,
			IMapper mapper,
			ILogger<AuthController> logger,
			IOtpService otpService,
			UserManager<User> userManager)
		{
			_authService = authService;
			_mapper = mapper;
			_logger = logger;
			_otpService = otpService;
			_userManager = userManager;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register(RegisterRequest request)
		{
			var command = _mapper.Map<RegisterCommand>(request);
			var result = await _authService.RegisterAsync(command);
			return ResultHandler(result);
		}

		[HttpPost("confirm-email")]
		public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
		{
			var command = _mapper.Map<ConfirmEmailCommand>(request);
			var result = await _authService.ConfirmEmailAsync(command);
			return ResultHandler(result);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login(LoginRequest request)
		{
			var command = _mapper.Map<LoginCommand>(request);
			var result = await _authService.LoginAsync(command);
			return ResultHandler(result);
		}

		[HttpPost("assign-hospital-role")]
		// Temporarily removing authorization for testing - TODO: Add back [Authorize(Roles = "Admin")]
		public async Task<IActionResult> AssignHospitalRole([FromBody] AssignRoleRequest request)
		{
			try
			{
				var user = await _userManager.FindByEmailAsync(request.Email);
				if (user == null)
					return NotFound(new { success = false, message = "User not found" });

				var result = await _userManager.AddToRoleAsync(user, "Hospital");
				if (result.Succeeded)
				{
					_logger.LogInformation("Hospital role assigned to user {Email}", request.Email);
					return Ok(new { success = true, message = "Hospital role assigned successfully" });
				}

				return BadRequest(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error assigning hospital role to {Email}", request.Email);
				return StatusCode(500, new { success = false, message = "Error assigning role" });
			}
		}

		[HttpPost("request-password-reset")]
		public async Task<IActionResult> RequestPasswordReset([FromBody] string email)
		{
			var result = await _authService.InitiatePasswordResetAsync(email);
			return ResultHandler(result);
		}

		[HttpPost("reset-password")]
		public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
		{
			var command = _mapper.Map<ResetPasswordCommand>(request);
			var result = await _authService.CompletePasswordResetAsync(command);
			return ResultHandler(result);
		}

		private IActionResult ResultHandler(AuthResult result)
		{
			if (result.Success)
			{
				_logger.LogInformation("Operation succeeded");
				var response = _mapper.Map<AuthResponse>(result);
				return Ok(response);
			}

			_logger.LogWarning("Operation failed: {Errors}", result.Message);
			return BadRequest(new
			{
				Success = false,
				Message = result.Message
			});
		}

		public class AssignRoleRequest
		{
			public string Email { get; set; }
		}
	}
}

