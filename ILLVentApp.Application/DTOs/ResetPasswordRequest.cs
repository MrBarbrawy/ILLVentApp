using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Application.DTOs
{
	public class ResetPasswordRequest
	{
		[Required, EmailAddress]
		public string Email { get; set; }

		[Required, DataType(DataType.Password)]
		public string NewPassword { get; set; }

		[Compare("NewPassword")]
		public string ConfirmNewPassword { get; set; }

		[Required]
		public string Otp { get; set; }
	}
}
