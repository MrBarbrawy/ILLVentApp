using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Application.DTOs
{
	public class RegisterRequest
	{
		[Required, MaxLength(50)]
		public string FirstName { get; set; }

		[Required, MaxLength(50)]
		public string Surname { get; set; }

		[Required, EmailAddress]
		public string Email { get; set; }

		[Required, DataType(DataType.Password)]
		public string Password { get; set; }

		[Compare("Password")]
		public string ConfirmPassword { get; set; }
	}
}
