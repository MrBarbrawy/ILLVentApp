using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.DTOs
{
	public class ResetPasswordCommand
	{
		public string Email { get; set; }
		public string NewPassword { get; set; }
		public string Otp { get; set; }
	}
}
