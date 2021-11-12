using System;
using System.Threading.Tasks;
using Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

public sealed class CosmosRequestChargeOperations : ICosmosRequestChargeOperations
{
	private double requestCharges_ = 0.0D;
	private readonly ICosmosOperations operations_;
	private readonly ILogger logger_;

	public double RequestCharges => requestCharges_;

	public CosmosRequestChargeOperations(CosmosClient client)
		: this(new CosmosOperations(client), null)
	{
	}
	public CosmosRequestChargeOperations(CosmosClient client, ILogger<CosmosRequestChargeOperations> logger)
		: this(new CosmosOperations(client), logger)
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
		var response = await operations_.CreateDatabaseIfNotExistsAsync(databaseName);
		AccumulateRequestCharges(response.RequestCharge);
		logger_.TraceRequestCharge(nameof(ICosmosOperations.CreateDatabaseIfNotExistsAsync), response.RequestCharge);
		return response;
	}

	async Task<ContainerResponse> ICosmosOperations.CreateContainerIfNotExistsAsync(Database database, string containerName, string partitionPath)
	{
		var response = await operations_.CreateContainerIfNotExistsAsync(database, containerName, partitionPath);
		AccumulateRequestCharges(response.RequestCharge);
		logger_.TraceRequestCharge(nameof(ICosmosOperations.CreateContainerIfNotExistsAsync), response.RequestCharge);
		return response;
	}

	private void AccumulateRequestCharges(double requestCharge)
	{
		requestCharges_ += requestCharge;
	}
}
