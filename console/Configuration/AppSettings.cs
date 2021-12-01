public sealed class AppSettings
{
	public sealed class CosmosDbSettings {
		public string EndpointUri { get; set; }
		public string PrimaryKey { get; set; }
	}

	public CosmosDbSettings CosmosDb { get; set; } = default;
}