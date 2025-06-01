using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Application.DTOs
{
	public class AuthResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string Token { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }
		public string UserName { get; set; }
	}
}
