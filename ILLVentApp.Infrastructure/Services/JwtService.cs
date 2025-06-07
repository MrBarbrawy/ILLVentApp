using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Infrastructure.Services
{
	public class JwtService : IJwtService
	{
		private readonly IConfiguration _config;

		public JwtService(IConfiguration config)
		{
			_config = config;
		}

		public string GenerateToken(User user)
		{
			return GenerateToken(user, new List<string>());
		}

		public string GenerateToken(User user, IList<string> roles)
		{
			var claims = new List<Claim>
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Id),
			new Claim(JwtRegisteredClaimNames.Email, user.Email),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(ClaimTypes.NameIdentifier, user.Id),
			new Claim(ClaimTypes.GivenName, $"{user.FirstName} {user.Surname}"),
			new Claim("email_verified", user.IsEmailVerified.ToString())
		};

			// Add role claims using short claim type to match JWT Bearer configuration
			foreach (var role in roles)
			{
				claims.Add(new Claim("role", role));
			}

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
				_config["Jwt:Key"]));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _config["Jwt:Issuer"],
				audience: _config["Jwt:Audience"],
				claims: claims,
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
