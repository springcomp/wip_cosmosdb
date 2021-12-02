using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Utils.CosmosDb.Interop;
using Utils.CosmosDb.Logging;

namespace Utils.CosmosDb
{
    public sealed class CosmosRequestChargeOperations : ICosmosRequestChargeOperations
    {
        private double requestCharges_ = 0.0D;
        private readonly ICosmosOperations operations_;
        private readonly ILogger logger_;

        public double RequestCharges => requestCharges_;

        public CosmosRequestChargeOperations(CosmosClient client)
            : this(new CosmosOperations(client, null), null)
        {
        }
        public CosmosRequestChargeOperations(CosmosClient client, ILogger<CosmosRequestChargeOperations> logger)
            : this(new CosmosOperations(client, (ILogger)logger), logger)
        {
        }
        public CosmosRequestChargeOperations(ICosmosOperations operations)
            : this(operations, null)
        {
        }
        public CosmosRequestChargeOperations(ICosmosOperations operations, ILogger<CosmosRequestChargeOperations> logger)
        {
            operations_ = operations;
            logger_ = (ILogger)logger ?? new NoOpLogger();
        }

        public async Task<DatabaseResponse> CreateDatabaseIfNotExistsAsync(string databaseName)
        {
            return await CalcRequestCharges<DatabaseResponse>(
                nameof(ICosmosOperations.CreateDatabaseIfNotExistsAsync),
                async () => await operations_.CreateDatabaseIfNotExistsAsync(databaseName),
                r => r.RequestCharge
            );
        }

        async Task<ContainerResponse> ICosmosOperations.CreateContainerIfNotExistsAsync(Database database, string containerName, string partitionPath)
        {
            return await CalcRequestCharges<ContainerResponse>(
                nameof(ICosmosOperations.CreateContainerIfNotExistsAsync),
                async () => await operations_.CreateContainerIfNotExistsAsync(database, containerName, partitionPath),
                r => r.RequestCharge
                );
        }
        public async Task<ItemResponse<T>> GetItemAsync<T>(Container container, string partition, string id) where T : ICosmosDbItem
        {
            return await CalcRequestCharges<ItemResponse<T>>(
                nameof(ICosmosOperations.GetItemAsync),
                async () => await operations_.GetItemAsync<T>(container, partition, id),
                r => r.RequestCharge
            );
        }

        public async Task<ItemResponse<T>> CreateItemAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem
        {
            return await CalcRequestCharges<ItemResponse<T>>(
                nameof(ICosmosOperations.CreateItemAsync),
                async () => await operations_.CreateItemAsync(container, item, partition),
                r => r.RequestCharge
            );
        }

        public async Task<ItemResponse<T>> ReplaceItemAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem
        {
            return await CalcRequestCharges<ItemResponse<T>>(
                nameof(ICosmosOperations.CreateItemAsync),
                async () => await operations_.ReplaceItemAsync(container, item, partition),
                r => r.RequestCharge
            );
        }

        public async Task<ItemResponse<T>> CreateOrReplaceItemAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem
        {
            try
            {
                var response = await GetItemAsync<T>(container, partition, item.Id);
                return await ReplaceItemAsync<T>(container, item, partition);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return await CreateItemAsync<T>(container, item, partition);
            }
        }

        public async Task<ItemResponse<T>> CreateItemIfNotExistsAsync<T>(Container container, T item, string partition) where T : ICosmosDbItem
        {
            try
            {
                return await CreateItemAsync<T>(container, item, partition);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                return await GetItemAsync<T>(container, partition, item.Id);
            }
        }

        private async Task<T> CalcRequestCharges<T>(string name, Func<Task<T>> function, Func<T, double> getRequestCharge)
        {
            T response = default(T);
            try
            {
                response = await function();
                return response;
            }
            finally
            {
                if (response != null)
                {
                    var requestCharge = getRequestCharge(response);
                    logger_.TraceRequestCharge(name, requestCharge);
                    AccumulateRequestCharges(name, requestCharge);
                }
            }
        }

        private void AccumulateRequestCharges(string name, double requestCharge)
        {
            logger_.TraceRequestCharge(name, requestCharge);
            requestCharges_ += requestCharge;
        }
    }
}
