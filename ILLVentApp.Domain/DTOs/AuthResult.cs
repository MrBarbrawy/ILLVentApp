using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.DTOs
{
	public class AuthResult
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string Token { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }
		public string UserName { get; set; }

		public static AuthResult success(string token = null, string email = null, string role = null, string userName = null) => new()
		{
			Success = true,
			Message = "Operation succeeded",
			Token = token,
			Email = email,
			Role = role,
			UserName = userName
		};

		public static AuthResult Failure(IEnumerable<string> errors) => new()
		{
			Success = false,
			Message = string.Join(", ", errors)
		};

		public static AuthResult Failure(string error) => new()
		{
			Success = false,
			Message = error
		};
	}
}
