using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.DTOs
{
	public class ConfirmEmailCommand
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[StringLength(6, MinimumLength = 6)] // For 6-digit OTP
		public string Otp { get; set; } // Raw OTP from user input
	}
}
