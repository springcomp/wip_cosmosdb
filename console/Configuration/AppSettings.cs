public sealed class AppSettings
{
	public sealed class CosmosDbSettings {
		public string EndpointUri { get; set; }
		public string PrimaryKey { get; set; }
		public bool IgnoreSslServerCertificateValidation { get; set; } = false;
	}

	public CosmosDbSettings CosmosDb { get; set; } = default;
}