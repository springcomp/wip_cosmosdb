using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public interface ICosmosOperations {
	Task<DatabaseResponse> CreateDatabaseIfNotExistsAsync(string databaseName);
	Task<ContainerResponse> CreateContainerIfNotExistsAsync(Database database, string containerName, string partitionPath);
}
