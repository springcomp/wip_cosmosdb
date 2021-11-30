using System;
using Microsoft.Extensions.Logging;

namespace Utils.CosmosDb.Logging
{
	internal sealed class NoOpLogger : ILogger
	{
		public IDisposable BeginScope<TState>(TState state)
		{
			return new Disposable();
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return false;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
		}

		private sealed class Disposable : IDisposable { public void Dispose() { } }
	}
}