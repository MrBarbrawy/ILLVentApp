using ILLVentApp.Domain.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Infrastructure.Services
{
	public class LocalLockProvider : IDistributedLockProvider
	{
		private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

		public async Task<ILockHandle> AcquireLockAsync(string resource, TimeSpan timeout)
		{
			var semaphore = _locks.GetOrAdd(resource, _ => new SemaphoreSlim(1, 1));
			var acquired = await semaphore.WaitAsync(timeout);

			return new LocalLockHandle(semaphore, acquired);
		}

		private class LocalLockHandle : ILockHandle
		{
			private readonly SemaphoreSlim _semaphore;

			public LocalLockHandle(SemaphoreSlim semaphore, bool acquired)
			{
				_semaphore = semaphore;
				IsAcquired = acquired;
			}

			public bool IsAcquired { get; }

			public ValueTask DisposeAsync()
			{
				if (IsAcquired)
					_semaphore.Release();

				return ValueTask.CompletedTask;
			}
		}
	}
}
