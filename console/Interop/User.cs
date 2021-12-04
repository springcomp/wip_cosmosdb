using System;
using CosmosGettingStartedTutorial;
using Newtonsoft.Json;
using Utils.CosmosDb.Interop;

namespace Model.Interop
{
	public sealed class User : ICosmosDbItem
	{
		[JsonProperty("id")]
		public string Id { get; set; }
		[JsonProperty("displayName")]
		public string DisplayName { get; set; }
		[JsonProperty("emailAddress")]
		public string EmailAddress { get; set; }
		[JsonProperty("forwardingAddress")]
		public string ForwardingAddress { get; set; }

		[JsonProperty("createdUtc")]
		public DateTime CreatedUtc { get; set; }
		[JsonProperty("addresses")]
		public MaskedEmail[] Addresses { get; set; }

		public override string ToString()
		{
			return $"{Id}: {DisplayName} ({EmailAddress})";
		}
	}
}