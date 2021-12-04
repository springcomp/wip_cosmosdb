using System;
using Newtonsoft.Json;

namespace Model.Interop
{
	public class MaskedEmail
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
			target.Id = Id;
			target.Name = Name;
			target.Description = Description;
			target.EmailAddress = EmailAddress;
			target.EnableForwarding = EnableForwarding;
			target.Received = Received;
			target.CreatedUtc = CreatedUtc;
		}

		public override string ToString()
		{
			return $"{EmailAddress} ==> {EnableForwarding}";
		}
	}
}