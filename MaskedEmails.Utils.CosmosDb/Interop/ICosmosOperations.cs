using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Utils.CosmosDb.Interop
{
	public interface ICosmosOperations
	{
		Task<DatabaseResponse> CreateDatabaseIfNotExistsAsync(string databaseName);
		Task<ContainerResponse> CreateContainerIfNotExistsAsync(Database database, string containerName, string partitionPath);

		Task<ItemResponse<T>> GetItemAsync<T>(Container container, string partition, string id) where T : ICosmosDbItem;
		Task<ItemResponse<T>> CreateItemAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem;
		Task<ItemResponse<T>> ReplaceItemAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem;
		Task<ItemResponse<T>> CreateItemIfNotExistsAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem;
		Task<ItemResponse<T>> CreateOrReplaceItemAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem;
	}
}
