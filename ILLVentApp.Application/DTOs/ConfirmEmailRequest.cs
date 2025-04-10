﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Application.DTOs
{
	public class ConfirmEmailRequest
	{
		[Required, EmailAddress]
		public string Email { get; set; }

		[Required, StringLength(6, MinimumLength = 6)]
		public string Otp { get; set; }
	}
}
