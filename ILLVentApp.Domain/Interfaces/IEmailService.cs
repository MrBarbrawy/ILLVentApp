using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
	public interface IEmailService
	{
		Task SendVerificationEmailAsync(string email, string otp);
		Task SendPasswordResetEmailAsync(string email, string otp);
		Task SendSecurityAlertAsync(string email, string message);
	}
}
