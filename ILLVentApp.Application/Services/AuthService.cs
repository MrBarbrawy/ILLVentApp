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
			// Enhanced validation
			if (string.IsNullOrWhiteSpace(command?.Email) ||
				string.IsNullOrWhiteSpace(command.Password) ||
				string.IsNullOrWhiteSpace(command.FirstName) ||
				string.IsNullOrWhiteSpace(command.Surname))
			{
				return AuthResult.Failure("All fields are required");
			}

			// Validate email format explicitly
			if (!new EmailAddressAttribute().IsValid(command.Email))
			{
				return AuthResult.Failure("Invalid email format");
			}

			// Generate secure OTP pair
			var (otpCode, otpHash) = _otpService.GenerateSecureOtp();

			var user = new User
			{
				// Required by Identity
				UserName = command.Email.Trim().Normalize(),
				NormalizedUserName = command.Email.Trim().ToUpperInvariant(),
				Email = command.Email.Trim(),
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

				// Check for common registration errors and provide user-friendly messages
				var errors = result.Errors.Select(e => e.Description).ToList();
				
				// Check for duplicate email/username
				if (errors.Any(e => e.Contains("Email") && e.Contains("taken")) ||
					errors.Any(e => e.Contains("Username") && e.Contains("taken")) ||
					errors.Any(e => e.Contains("already taken")))
				{
					return AuthResult.Failure("An account with this email already exists");
				}
				
				// Check for password policy violations
				if (errors.Any(e => e.Contains("password") && (e.Contains("length") || e.Contains("character") || e.Contains("digit") || e.Contains("uppercase") || e.Contains("lowercase") || e.Contains("special"))))
				{
					return AuthResult.Failure("Password must meet security requirements");
				}
				
				// Check for invalid email format (though should be caught earlier)
				if (errors.Any(e => e.Contains("Email") && e.Contains("valid")))
				{
					return AuthResult.Failure("Please enter a valid email address");
				}
				
				// Default fallback for other errors
				return AuthResult.Failure("Registration failed. Please check your information and try again");
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
		try
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
				{
					_logger.LogWarning("Too many confirmation attempts for email: {Email}", command.Email);
					return AuthResult.Failure("Please wait a moment before trying again");
				}

				var user = await _userManager.FindByEmailAsync(command.Email);
				if (user == null)
				{
					_logger.LogWarning("Email confirmation attempt for non-existent user: {Email}", command.Email);
					return AuthResult.Failure("Account not found. Please check your email address");
				}

				if (user.IsEmailVerified)
				{
					_logger.LogInformation("Email already verified for user: {UserId}", user.Id);
					return AuthResult.success(email: user.Email);
				}

				var validationResult = await ValidateOtp(user, command.Otp);
				if (!validationResult.Success) return validationResult;

				return await UpdateUserState(user);
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Email confirmation failed for {Email}", command?.Email);
			return AuthResult.Failure("Something went wrong. Please try again");
		}
	}


	private AuthResult? ValidateCommand(ConfirmEmailCommand command)
	{
		if (command == null)
		{
			_logger.LogWarning("Null confirmation command received");
			return AuthResult.Failure("Invalid confirmation request");
		}

		if (string.IsNullOrWhiteSpace(command.Email))
		{
			return AuthResult.Failure("Email address is required");
		}

		if (!new EmailAddressAttribute().IsValid(command.Email))
		{
			return AuthResult.Failure("Please enter a valid email address");
		}

		if (string.IsNullOrWhiteSpace(command.Otp))
		{
			return AuthResult.Failure("Verification code is required");
		}

		if (command.Otp.Length != 6)
		{
			return AuthResult.Failure("Verification code must be 6 digits");
		}

		return null;
	}

	private async Task<AuthResult> ValidateOtp(User user, string otp)
	{
		if (string.IsNullOrEmpty(user.Otp))
		{
			_logger.LogWarning("No active OTP for user: {UserId}", user.Id);
			return AuthResult.Failure("No verification code found. Please request a new one");
		}

		if (_otpService.IsExpired(user.OtpExpiry))
		{
			_logger.LogWarning("Expired OTP attempt for user: {UserId}", user.Id);
			return AuthResult.Failure("Verification code has expired. Please request a new one");
		}

		if (!_otpService.Validate(otp, user.Otp))
		{
			_logger.LogWarning("Invalid OTP attempt for user: {UserId}", user.Id);
			await _userManager.AccessFailedAsync(user);
			return AuthResult.Failure("Invalid verification code. Please check and try again");
		}

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
				return AuthResult.Failure("Email verification failed. Please try again");
			}

			try
			{
				await _emailService.SendSecurityAlertAsync(user.Email!, "Email confirmed");
			}
			catch (Exception ex)
			{
				// Don't fail verification if security email fails
				_logger.LogWarning(ex, "Failed to send security alert for user: {UserId}", user.Id);
			}

			_logger.LogInformation("Email successfully verified for user: {UserId}", user.Id);
			return AuthResult.success(email: user.Email);
		}
		catch (DbUpdateConcurrencyException ex)
		{
			_logger.LogWarning(ex, "Concurrency conflict updating user: {UserId}", user.Id);
			return AuthResult.Failure("Verification failed due to conflict. Please try again");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating user state for: {UserId}", user.Id);
			return AuthResult.Failure("Email verification failed. Please try again");
		}
	}

	private void LogUpdateErrors(IEnumerable<IdentityError> errors, string userId)
	{
		_logger.LogError("User update failed for {UserId}: {Errors}",
			userId, string.Join(", ", errors.Select(e => e.Description)));
	}


	public async Task<AuthResult> LoginAsync(LoginCommand command)
	{
		try
		{
			// Basic input validation
			if (string.IsNullOrWhiteSpace(command?.Email) || 
				string.IsNullOrWhiteSpace(command.Password))
			{
				return AuthResult.Failure("Please enter both email and password");
			}

			// Validate email format
			if (!new EmailAddressAttribute().IsValid(command.Email))
			{
				return AuthResult.Failure("Please enter a valid email address");
			}

			var user = await _userManager.FindByEmailAsync(command.Email);
			if (user == null)
			{
				// Log failed attempt but don't reveal user doesn't exist
				_logger.LogWarning("Login attempt for non-existent user: {Email}", command.Email);
				return AuthResult.Failure("Invalid email or password");
			}

			// Check if account is locked out
			if (await _userManager.IsLockedOutAsync(user))
			{
				_logger.LogWarning("Login attempt for locked out user: {UserId}", user.Id);
				return AuthResult.Failure("Account is temporarily locked. Please try again later");
			}

			// Check password
			if (!await _userManager.CheckPasswordAsync(user, command.Password))
			{
				// Record failed attempt (this may trigger lockout)
				await _userManager.AccessFailedAsync(user);
				_logger.LogWarning("Invalid password attempt for user: {UserId}", user.Id);
				return AuthResult.Failure("Invalid email or password");
			}

			// Reset access failed count on successful password check
			await _userManager.ResetAccessFailedCountAsync(user);

			// Check if email is verified
			if (!user.IsEmailVerified)
			{
				_logger.LogWarning("Login attempt for unverified user: {UserId}", user.Id);
				return AuthResult.Failure("Please verify your email address first");
			}

			// Get user roles
			var roles = await _userManager.GetRolesAsync(user);
			var primaryRole = roles.FirstOrDefault() ?? "User";

			// Generate JWT token
			var token = _jwtService.GenerateToken(user);
			if (string.IsNullOrEmpty(token))
			{
				_logger.LogError("Failed to generate token for user: {UserId}", user.Id);
				return AuthResult.Failure("Login failed. Please try again");
			}

			var userName = $"{user.FirstName} {user.Surname}";
			
			_logger.LogInformation("Successful login for user: {UserId}", user.Id);
			return AuthResult.success(token: token, email: user.Email, role: primaryRole, userName: userName);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Login failed for {Email}", command?.Email);
			return AuthResult.Failure("Something went wrong. Please try again");
		}
	}

	public async Task<AuthResult> InitiatePasswordResetAsync(string email)
	{
		try
		{
			// Input validation
			if (string.IsNullOrWhiteSpace(email))
			{
				return AuthResult.Failure("Email address is required");
			}

			if (!new EmailAddressAttribute().IsValid(email))
			{
				return AuthResult.Failure("Please enter a valid email address");
			}

			var user = await _userManager.FindByEmailAsync(email);
			if (user == null)
			{
				// Don't reveal if user exists - return success for security
				_logger.LogWarning("Password reset requested for non-existent user: {Email}", email);
				return AuthResult.success(email: email);
			}

			// Check if user is locked out
			if (await _userManager.IsLockedOutAsync(user))
			{
				_logger.LogWarning("Password reset requested for locked out user: {UserId}", user.Id);
				return AuthResult.Failure("Account is temporarily locked. Please try again later");
			}

			// Generate new OTP
			var (code, hash) = _otpService.GenerateSecureOtp();
			user.Otp = hash;
			user.OtpExpiry = DateTime.UtcNow.AddMinutes(15);
			
			var updateResult = await _userManager.UpdateAsync(user);
			if (!updateResult.Succeeded)
			{
				_logger.LogError("Failed to update user OTP for password reset: {UserId}", user.Id);
				return AuthResult.Failure("Password reset failed. Please try again");
			}

			// Send reset email
			try
			{
				await _emailService.SendPasswordResetEmailAsync(email, code);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send password reset email to: {Email}", email);
				return AuthResult.Failure("Failed to send reset email. Please try again");
			}

			_logger.LogInformation("Password reset initiated for user: {UserId}", user.Id);
			return AuthResult.success(email: email);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Password reset initiation failed for email: {Email}", email);
			return AuthResult.Failure("Something went wrong. Please try again");
		}
	}

	public async Task<AuthResult> CompletePasswordResetAsync(ResetPasswordCommand command)
	{
		try
		{
			// Input validation
			if (command == null)
			{
				return AuthResult.Failure("Invalid reset request");
			}

			if (string.IsNullOrWhiteSpace(command.Email))
			{
				return AuthResult.Failure("Email address is required");
			}

			if (!new EmailAddressAttribute().IsValid(command.Email))
			{
				return AuthResult.Failure("Please enter a valid email address");
			}

			if (string.IsNullOrWhiteSpace(command.Otp))
			{
				return AuthResult.Failure("Verification code is required");
			}

			if (string.IsNullOrWhiteSpace(command.NewPassword))
			{
				return AuthResult.Failure("New password is required");
			}

			// Find user
			var user = await _userManager.FindByEmailAsync(command.Email);
			if (user == null)
			{
				_logger.LogWarning("Password reset attempt for non-existent user: {Email}", command.Email);
				return AuthResult.Failure("Account not found");
			}

			// Check if OTP has expired
			if (user.OtpExpiry == null || user.OtpExpiry < DateTime.UtcNow)
			{
				_logger.LogWarning("Expired password reset attempt for user: {UserId}", user.Id);
				return AuthResult.Failure("Reset code has expired. Please request a new one");
			}

			// Validate OTP
			if (string.IsNullOrEmpty(user.Otp) || !_otpService.Validate(command.Otp, user.Otp))
			{
				_logger.LogWarning("Invalid password reset code for user: {UserId}", user.Id);
				await _userManager.AccessFailedAsync(user);
				return AuthResult.Failure("Invalid reset code. Please check and try again");
			}

			// Generate password reset token and reset password
			var token = await _userManager.GeneratePasswordResetTokenAsync(user);
			var result = await _userManager.ResetPasswordAsync(user, token, command.NewPassword);

			if (!result.Succeeded)
			{
				_logger.LogWarning("Password reset failed for user {UserId}: {Errors}", 
					user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
				
				// Check for common password policy violations
				var errors = result.Errors.Select(e => e.Description).ToList();
				if (errors.Any(e => e.Contains("password") && (e.Contains("length") || e.Contains("character") || e.Contains("digit"))))
				{
					return AuthResult.Failure("Password must meet security requirements");
				}
				
				return AuthResult.Failure("Password reset failed. Please try again");
			}

			// Clean up OTP
			try
			{
				user.Otp = null;
				user.OtpExpiry = null;
				user.SecurityVersion++;
				
				var updateResult = await _userManager.UpdateAsync(user);
				if (!updateResult.Succeeded)
				{
					_logger.LogWarning("Failed to clean up OTP after password reset for user: {UserId}", user.Id);
					// Don't fail the operation, password was already reset
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to clean up OTP after password reset for user: {UserId}", user.Id);
				// Don't fail the operation, password was already reset
			}

			// Send security alert
			try
			{
				await _emailService.SendSecurityAlertAsync(user.Email!, "Password changed");
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to send security alert for password reset: {UserId}", user.Id);
				// Don't fail the operation
			}

			_logger.LogInformation("Password successfully reset for user: {UserId}", user.Id);
			return AuthResult.success(email: user.Email);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Password reset completion failed for email: {Email}", command?.Email);
			return AuthResult.Failure("Something went wrong. Please try again");
		}
	}
}
