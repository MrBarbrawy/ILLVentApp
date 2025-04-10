using ILLVentApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using ILLVentApp.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
namespace ILLVentApp.Infrastructure.Services
{
	public class EmailService : IEmailService
	{
		private readonly EmailSettings _settings;
		private readonly ILogger<EmailService> _logger;

		public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
		{
			_settings = settings.Value;
			_logger = logger;
		}

		public async Task SendVerificationEmailAsync(string email, string otp)
		{
			var subject = "Verify Your Email Address";
			var body = $"""
            <h1>Email Verification</h1>
            <p>Your verification code is: <strong>{otp}</strong></p>
            <p>This code expires in 20 minutes.</p>
            """;

			await SendEmailAsync(email, subject, body);
		}

		public async Task SendPasswordResetEmailAsync(string email, string otp)
		{
			var subject = "Password Reset Request";
			var body = $"""
            <h1>Password Reset</h1>
            <p>Your reset code is: <strong>{otp}</strong></p>
            <p>This code expires in 20 minutes.</p>
            """;

			await SendEmailAsync(email, subject, body);
		}

		public async Task SendSecurityAlertAsync(string email, string message)
		{
			var subject = "Security Alert";
			var body = $"""
            <h1>Security Notice</h1>
            <p>{message}</p>
            """;

			await SendEmailAsync(email, subject, body);
		}

		public async Task SendEmailAsync(string email, string subject, string body)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
			message.To.Add(new MailboxAddress("", email));
			message.Subject = subject;

			message.Body = new TextPart("html")
			{
				Text = body
			};

			using var client = new SmtpClient();
			await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, _settings.UseSsl);
			await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
			await client.SendAsync(message);
			await client.DisconnectAsync(true);
		}
	}
}
