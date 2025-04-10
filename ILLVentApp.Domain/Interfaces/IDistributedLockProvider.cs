using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
	public interface IDistributedLockProvider
	{
		Task<ILockHandle> AcquireLockAsync(string resource, TimeSpan timeout);
	}
	public interface ILockHandle : IAsyncDisposable
	{
		bool IsAcquired { get; }
	}
}
