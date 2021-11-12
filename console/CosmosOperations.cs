using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public sealed class CosmosOperations : ICosmosOperations {
	private readonly CosmosClient client_;

	public CosmosOperations(string endpoint, string primaryKey)
		: this (endpoint, primaryKey, new CosmosClientOptions())
	{
	}

	public CosmosOperations(string endpoint, string primaryKey, CosmosClientOptions clientOptions)
		: this (new CosmosClient(endpoint, primaryKey, clientOptions))
	{}

	public CosmosOperations(CosmosClient client)
	{
		client_ = client;
	}

	public async Task<DatabaseResponse> CreateDatabaseIfNotExistsAsync(string databaseName)
	{
		var database = await client_.CreateDatabaseIfNotExistsAsync(databaseName);
		return database;

	}
	public async Task<ContainerResponse> CreateContainerIfNotExistsAsync(Database database, string containerName, string partitionPath)
	{
		var container = await database.CreateContainerIfNotExistsAsync(containerName, partitionPath);
		return container;
	}
}
