using System;
using Newtonsoft.Json;
using Utils.CosmosDb.Interop;

namespace Models
{
	public class MaskedEmail : ICosmosDbItem
	{
		[JsonProperty("id")]
		public string Id { get; set; }
		public string UserId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string EmailAddress { get; set; }
		public string ForwardToEmailAddress { get; set; }

		// statistics

		public int Received { get; set; }

		public DateTime CreatedUtc { get; set; }

		public void CloneTo(MaskedEmail target)
		{
			target.Name = Name;
			target.Description = Description;
			target.EmailAddress = EmailAddress;
			target.ForwardToEmailAddress = ForwardToEmailAddress;
			target.Received = Received;
			target.CreatedUtc = CreatedUtc;
		}
		public static MaskedEmail Clone(Model.Interop.MaskedEmail source)
		{
			var target = new MaskedEmail {
				Name = source.Name,
				Description = source.Description,
				EmailAddress = source.EmailAddress,
				ForwardToEmailAddress = source.ForwardToEmailAddress,
				Received = source.Received,
				CreatedUtc = source.CreatedUtc,
			};

			return target;
		}

		public override string ToString()
		{
			return $"{EmailAddress} ==> {ForwardToEmailAddress}";
		}
	}
}