using System;
using CosmosGettingStartedTutorial;
using Newtonsoft.Json;

namespace Model.Interop
{
	public sealed class User : ICosmosDbItem
	{
		[JsonProperty("id")]
		public string Id { get; set; }
		public string DisplayName { get; set; }
		public string ForwardingAddress { get; set; }

		public DateTime CreatedUtc { get; set; }
		public MaskedEmail[] Addresses { get; set; }
	}
}