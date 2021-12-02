using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Utils.CosmosDb.Interop;
using Utils.CosmosDb.Logging;

namespace Utils.CosmosDb
{
    public sealed class CosmosOperations : ICosmosOperations
    {
        private readonly CosmosClient client_;
        private readonly ILogger logger_;

        public CosmosOperations(string endpoint, string primaryKey, ILogger<CosmosOperations> logger = null)
            : this(endpoint, primaryKey, new CosmosClientOptions(), logger)
        {
        }

        public CosmosOperations(string endpoint, string primaryKey, CosmosClientOptions clientOptions, ILogger<CosmosOperations> logger = null)
            : this(new CosmosClient(endpoint, primaryKey, clientOptions), logger)
        { }

        public CosmosOperations(CosmosClient client, ILogger<CosmosOperations> logger)
            : this(client, (ILogger)logger)
        {
        }

        public CosmosOperations(CosmosClient client, ILogger logger)
        {
            client_ = client;
            logger_ = logger ?? new NoOpLogger();
        }

        public async Task<DatabaseResponse> CreateDatabaseIfNotExistsAsync(string databaseName)
        {
            logger_.LogDebug($"CosmosDb: creating database {databaseName}.");
            var database = await client_.CreateDatabaseIfNotExistsAsync(databaseName);
            logger_.LogTrace($"CosmosDb: database {database.Database.Id} created successfully.");
            return database;

        }
        public async Task<ContainerResponse> CreateContainerIfNotExistsAsync(Database database, string containerName, string partitionPath)
        {
            logger_.LogDebug($"CosmosDb: creating container {database.Id}/{containerName}.");
            var container = await database.CreateContainerIfNotExistsAsync(containerName, partitionPath, 400);
            logger_.LogTrace($"CosmosDb: container {database.Id}/{container.Container.Id} created successfully.");
            return container;
        }

        public async Task<ItemResponse<T>> GetItemAsync<T>(Container container, string partition, string id) where T : ICosmosDbItem
        {
            var response = await container.ReadItemAsync<T>(id, new PartitionKey(partition));
            return response;
        }

        public async Task<ItemResponse<T>> CreateItemAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem
        {
            var response = await container.CreateItemAsync(item, new PartitionKey(partition));
            return response;
        }

        public async Task<ItemResponse<T>> ReplaceItemAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem
        {
            var response = await container.ReplaceItemAsync<T>(item, item.Id, new PartitionKey(partition));
            return response;
        }

        public async Task<ItemResponse<T>> CreateOrReplaceItemAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem
        {
            try
            {
                var response = await GetItemAsync<T>(container, item.Id, partition);
                response = await ReplaceItemAsync<T>(container, item, partition);
                return response;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var response = await CreateItemAsync<T>(container, item, partition);
                return response;
            }
        }

        public async Task<ItemResponse<T>> CreateItemIfNotExistsAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem
        {
            try
            {
                var response = await CreateItemAsync<T>(container, item, partition);
                return response;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                var response = await GetItemAsync<T>(container, item.Id, partition);
                return response;
            }
        }
    }
}
