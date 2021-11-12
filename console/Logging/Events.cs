using Microsoft.Extensions.Logging;

namespace Logging {
	public sealed class Templates {
		public const string RequestChargeOperation = "Message={Message}, RequestCharge={RequestCharge}";
	}

	public sealed class Events {
		private static readonly EventId trace_ = new EventId(0, "trace");

		public static EventId Trace => trace_;
	}
}