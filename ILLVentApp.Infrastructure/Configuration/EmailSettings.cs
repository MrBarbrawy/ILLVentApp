using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Infrastructure.Configuration
{
	public class EmailSettings
	{
		public const string SectionName = "EmailSettings";

		public string SenderName { get; set; } = string.Empty;
		public string SenderEmail { get; set; } = string.Empty;
		public string SmtpServer { get; set; } = string.Empty;
		public int SmtpPort { get; set; }
		public bool UseSsl { get; set; }
		public string SmtpUsername { get; set; } = string.Empty;
		public string SmtpPassword { get; set; } = string.Empty;
	}
}
