using System;
using Newtonsoft.Json;
using CosmosDb.Utils.Interop;

namespace Models
{
	public class MaskedEmail : ICosmosDbItem
	{
		[JsonProperty("id")]
		public string Id { get; set; }
		[JsonProperty("name")]
		public string Name { get; set; }
		[JsonProperty("description")]
		public string Description { get; set; }
		[JsonProperty("emailAddress")]
		public string EmailAddress { get; set; }
		[JsonProperty("enableForwarding")]
		public bool EnableForwarding { get; set; }

		// statistics

		[JsonProperty("received")]
		public int Received { get; set; }

		[JsonProperty("createdUtc")]
		public DateTime CreatedUtc { get; set; }

		public void CloneTo(MaskedEmail target)
		{
			target.Name = Name;
			target.Description = Description;
			target.EmailAddress = EmailAddress;
			target.EnableForwarding = EnableForwarding;
			target.Received = Received;
			target.CreatedUtc = CreatedUtc;
		}
		public static MaskedEmail Clone(Model.Interop.MaskedEmail source)
		{
			var target = new MaskedEmail {
				Name = source.Name,
				Description = source.Description,
				EmailAddress = source.EmailAddress,
				EnableForwarding = source.EnableForwarding,
				Received = source.Received,
				CreatedUtc = source.CreatedUtc,
			};

			return target;
		}

		public override string ToString()
		{
			return $"{EmailAddress} ==> {EnableForwarding}";
		}
	}
}