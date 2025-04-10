using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
	public interface IOtpService
	{
		(string Code, string Hash) GenerateSecureOtp();
		string HashOtp(string otp); // Add this line
		bool Validate(string inputOtp, string storedHash);
		bool IsExpired(DateTime? otpExpiry);
		void ValidateOtpInput(string otp);
	}
}
