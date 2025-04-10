using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.DTOs
{
	public class LoginCommand
	{
		public string Email { get; set; }
		public string Password { get; set; }
	}
}
