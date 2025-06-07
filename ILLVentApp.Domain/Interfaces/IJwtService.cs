using ILLVentApp.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
	public interface IJwtService
	{
		string GenerateToken(User user);
		string GenerateToken(User user, IList<string> roles);
	}
}
