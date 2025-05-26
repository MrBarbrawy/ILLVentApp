using AutoMapper;
using Microsoft.AspNetCore.Identity;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlTypes;

namespace ILLVentApp.Application.Services;

public class AuthService : IAuthService
{
	private readonly IMapper _mapper;
	private readonly UserManager<User> _userManager;
	private readonly IOtpService _otpService;
	private readonly IEmailService _emailService;
	private readonly ILogger<AuthService> _logger;
	private readonly IDistributedLockProvider _lockProvider;
	private readonly IRetryPolicyProvider _retryPolicy;
	private readonly IJwtService _jwtService;

	public AuthService(
		IMapper mapper,
		UserManager<User> userManager,
		IOtpService otpService,
		IEmailService emailService,
		ILogger<AuthService> logger,
		IDistributedLockProvider lockProvider,
		IRetryPolicyProvider retryPolicy,
		IJwtService jwtService)
	{
		_mapper = mapper;
		_userManager = userManager;
		_otpService = otpService;
		_emailService = emailService;
		_logger = logger;
		_lockProvider = lockProvider;
		_retryPolicy = retryPolicy;
		_jwtService = jwtService;
	}

	public async Task<AuthResult> RegisterAsync(RegisterCommand command)
	{
		try
		{
			// Validate input command
			if (string.IsNullOrWhiteSpace(command?.Email) ||
				string.IsNullOrWhiteSpace(command.Password))
			{
				return AuthResult.Failure("Email and password are required");
			}

			// Generate secure OTP pair
			var (otpCode, otpHash) = _otpService.GenerateSecureOtp();

			var user = new User
			{
				// Required by Identity
				UserName = command.Email.Trim().Normalize(),
				NormalizedUserName = command.Email.Trim().ToUpperInvariant(), // Add this
				Email = command.Email,
				NormalizedEmail = command.Email.Trim().ToUpperInvariant(),

				// Required custom fields (auto-populated)
				FirstName = command.FirstName.Trim(),
				Surname = command.Surname.Trim(),

				// Optional fields (managed later)
				Otp = otpHash,
				OtpExpiry = DateTime.UtcNow.AddMinutes(20)
			};

			// Validate normalized email
			if (string.IsNullOrWhiteSpace(user.NormalizedEmail))
			{
				throw new ArgumentException("Invalid email format");
			}

			// Create user with password
			var result = await _userManager.CreateAsync(user, command.Password);

			if (!result.Succeeded)
			{
				_logger.LogError("Registration failed for {Email}: {Errors}",
					command.Email,
					string.Join(", ", result.Errors.Select(e => e.Description)));

				return AuthResult.Failure(result.Errors.Select(e => e.Description));
			}

			// Assign default "User" role
			var roleResult = await _userManager.AddToRoleAsync(user, "User");
			if (!roleResult.Succeeded)
			{
				_logger.LogWarning("Failed to assign default role to user {UserId}: {Errors}",
					user.Id,
					string.Join(", ", roleResult.Errors.Select(e => e.Description)));
				// Don't fail registration if role assignment fails, just log it
			}

			// Send verification email
			await _emailService.SendVerificationEmailAsync(user.Email, otpCode);

			_logger.LogInformation("User {UserId} registered successfully with default role", user.Id);
			return AuthResult.success(email: user.Email);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Registration failed for {Email}", command?.Email);
			return AuthResult.Failure("Registration failed due to system error");
		}
	}

	public async Task<AuthResult> ConfirmEmailAsync(ConfirmEmailCommand command)
	{
		// Validate command
		var validationError = ValidateCommand(command);
		if (validationError != null) return validationError;

		return await _retryPolicy.ExecuteAsync(async () =>
		{
			await using var lockHandle = await _lockProvider.AcquireLockAsync(
				$"confirm-email:{command.Email}",
				TimeSpan.FromSeconds(3));

			if (!lockHandle.IsAcquired)
				return AuthResult.Failure("Too many attempts");

			var user = await _userManager.FindByEmailAsync(command.Email);
			if (user == null) return AuthResult.Failure("User not found");

			if (user.IsEmailVerified)
				return AuthResult.success(email: user.Email);

			var validationResult = await ValidateOtp(user, command.Otp);
			if (!validationResult.Success) return validationResult;

			return await UpdateUserState(user);
		});
	}


	private AuthResult? ValidateCommand(ConfirmEmailCommand command)
	{
		if (command == null ||
			!new EmailAddressAttribute().IsValid(command.Email) ||
			string.IsNullOrWhiteSpace(command.Otp) ||
			command.Otp.Length != 6)
		{
			_logger.LogWarning("Invalid confirmation request");
			return AuthResult.Failure("Invalid request format");
		}
		return null;
	}

	private async Task<AuthResult> ValidateOtp(User user, string otp)
	{
		if (string.IsNullOrEmpty(user.Otp))
			return AuthResult.Failure("No active OTP");

		if (!_otpService.Validate(otp, user.Otp))
		{
			_logger.LogWarning("Invalid OTP attempt for {UserId}", user.Id);
			await _userManager.AccessFailedAsync(user);
			return AuthResult.Failure("Invalid OTP");
		}

		if (_otpService.IsExpired(user.OtpExpiry))
			return AuthResult.Failure("OTP expired");

		return AuthResult.success();
	}


	private async Task<AuthResult> UpdateUserState(User user)
	{
		try
		{
			user.IsEmailVerified = true;
			user.Otp = null;
			user.OtpExpiry = null;
			user.SecurityVersion++;

			var result = await _userManager.UpdateAsync(user);

			if (!result.Succeeded)
			{
				LogUpdateErrors(result.Errors, user.Id);
				return AuthResult.Failure(result.Errors.Select(e => e.Description));
			}

			await _emailService.SendSecurityAlertAsync(user.Email!, "Email confirmed");
			return AuthResult.success(email: user.Email);
		}
		catch (DbUpdateConcurrencyException ex)
		{
			_logger.LogWarning(ex, "Concurrency conflict updating {UserId}", user.Id);
			return AuthResult.Failure("Confirmation failed - please retry");
		}
	}

	private void LogUpdateErrors(IEnumerable<IdentityError> errors, string userId)
	{
		_logger.LogError("User update failed for {UserId}: {Errors}",
			userId, string.Join(", ", errors.Select(e => e.Description)));
	}


	public async Task<AuthResult> LoginAsync(LoginCommand command)
	{
		var user = await _userManager.FindByEmailAsync(command.Email);
		if (user == null || !await _userManager.CheckPasswordAsync(user, command.Password))
			return AuthResult.Failure("Invalid credentials");

		if (!user.IsEmailVerified)
			return AuthResult.Failure("Email not verified");

		var token = _jwtService.GenerateToken(user);
		return AuthResult.success(token: token, email: user.Email);
	}

	public async Task<AuthResult> InitiatePasswordResetAsync(string email)
	{
		var user = await _userManager.FindByEmailAsync(email);
		if (user == null) return AuthResult.Failure("User not found");

		var (code, hash) = _otpService.GenerateSecureOtp();
		user.Otp = hash;
		user.OtpExpiry = DateTime.UtcNow.AddMinutes(15);
		await _userManager.UpdateAsync(user);

		await _emailService.SendPasswordResetEmailAsync(email, code);
		return AuthResult.success(email: email);
	}

	public async Task<AuthResult> CompletePasswordResetAsync(ResetPasswordCommand command)
	{
		var user = await _userManager.FindByEmailAsync(command.Email);
		if (user == null || user.OtpExpiry < DateTime.UtcNow)
			return AuthResult.Failure("Invalid request");

		if (!_otpService.Validate(command.Otp, user.Otp))
			return AuthResult.Failure("Invalid OTP");

		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		var result = await _userManager.ResetPasswordAsync(user, token, command.NewPassword);

		if (!result.Succeeded)
			return AuthResult.Failure(result.Errors.Select(e => e.Description));

		user.Otp = null;
		user.OtpExpiry = null;
		await _userManager.UpdateAsync(user);

		return AuthResult.success(email: user.Email);
	}
}
