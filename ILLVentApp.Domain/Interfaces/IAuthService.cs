using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
	public interface IAuthService
	{
		Task<AuthResult> RegisterAsync(RegisterCommand command);
		Task<AuthResult> ConfirmEmailAsync(ConfirmEmailCommand command);
		Task<AuthResult> LoginAsync(LoginCommand command);
		Task<AuthResult> InitiatePasswordResetAsync(string email);
		Task<AuthResult> CompletePasswordResetAsync(ResetPasswordCommand command);
	}
}
