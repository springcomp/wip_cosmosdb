using Microsoft.Extensions.Logging;

namespace Utils.CosmosDb.Logging {
	internal static class LoggingExtensions {
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