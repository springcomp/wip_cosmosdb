using Microsoft.Extensions.Logging;

namespace Logging {
	public static class LoggerExtensions {
		public static void TraceRequestCharge(this ILogger logger, string message, double requestCharge)
		{
			logger.LogTrace(
				Events.Trace,
				Templates.RequestChargeOperation,
				new object[]{
					message,
					requestCharge,
				}
			);
		}
	}
}