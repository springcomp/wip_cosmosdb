using System.Net;
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

	public async Task<ItemResponse<T>> GetItemAsync<T>(Container container, string partition, string id) where T : ICosmosDbItem
	{
		var response = await container.ReadItemAsync<T>(id, new PartitionKey(partition));
		return response;
	}

	public async Task<ItemResponse<T>> CreateItemAsync<T>(Container container, T item, string partition) where T: ICosmosDbItem
	{
		var response = await container.CreateItemAsync(item, new PartitionKey(partition));
		return response;
	}

	public async Task<ItemResponse<T>> ReplaceItemAsync<T>(Container container, T item, string partition) where T: ICosmosDbItem
	{
		var response = await container.ReplaceItemAsync<T>(item, item.Id, new PartitionKey(partition));
		return response;
	}

	public async Task<ItemResponse<T>> InsertOrUpdateItemAsync<T>(Container container, T item, string partition) where T: ICosmosDbItem
	{
		try
		{
			var response = await container.ReadItemAsync<T>(item.Id, new PartitionKey(partition));
			response = await container.ReplaceItemAsync<T>(item, item.Id, new PartitionKey(partition));
			return response;
		}
		catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			var response = await container.CreateItemAsync<T>(item, new PartitionKey(partition));
			return response;
		}
	}
}
