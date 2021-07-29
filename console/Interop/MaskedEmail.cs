using System;

namespace Model.Interop
{
	public class MaskedEmail
	{
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
	}
}