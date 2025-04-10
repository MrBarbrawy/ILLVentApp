﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILLVentApp.Domain.Interfaces
{
	public interface IRetryPolicyProvider
	{
		Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
	}
}
