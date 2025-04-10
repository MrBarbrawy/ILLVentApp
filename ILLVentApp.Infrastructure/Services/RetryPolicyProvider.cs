using ILLVentApp.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Infrastructure.Services
{
	public class RetryPolicyProvider : IRetryPolicyProvider
	{
		private readonly ILogger<RetryPolicyProvider> _logger;

		public RetryPolicyProvider(ILogger<RetryPolicyProvider> logger)
		{
			_logger = logger;
		}

		public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
		{
			const int maxRetries = 3;
			var retryCount = 0;
			var delay = TimeSpan.FromMilliseconds(500);

			while (true)
			{
				try
				{
					return await operation();
				}
				catch (Exception ex) when (IsTransientError(ex) && retryCount < maxRetries)
				{
					retryCount++;
					_logger.LogWarning(ex, "Retry {RetryCount} for operation", retryCount);
					await Task.Delay(delay);
					delay *= 2;
				}
			}
		}

		private static bool IsTransientError(Exception ex)
		{
			return ex is SqlException sqlEx && sqlEx.Number switch
			{
				// SQL Server transient error codes
				-2 or 20 or 64 or 233 or 10053 or 10054 or 10060 => true,
				_ => false
			};
		}
	}
}
