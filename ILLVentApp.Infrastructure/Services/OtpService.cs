using ILLVentApp.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Infrastructure.Services
{
	public class OtpService : IOtpService
	{
		private const int OTP_LENGTH = 6;
		private readonly TimeSpan _validityPeriod = TimeSpan.FromMinutes(20);

		public (string Code, string Hash) GenerateSecureOtp()
		{
			var code = RandomNumberGenerator.GetString("0123456789", OTP_LENGTH);
			return (code, HashOtp(code));
		}

		public string HashOtp(string otp) =>
			BCrypt.Net.BCrypt.EnhancedHashPassword(otp, workFactor: 13);

		public bool Validate(string inputOtp, string storedHash) =>
			BCrypt.Net.BCrypt.EnhancedVerify(inputOtp, storedHash);

		public bool IsExpired(DateTime? otpExpiry) =>
			!otpExpiry.HasValue || otpExpiry.Value < DateTime.UtcNow;

		public void ValidateOtpInput(string otp)
		{
			if (string.IsNullOrWhiteSpace(otp) || otp.Length != OTP_LENGTH)
				throw new ArgumentException("Invalid OTP format");
		}
	}
}
