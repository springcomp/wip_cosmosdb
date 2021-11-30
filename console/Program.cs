using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using CosmosGettingStartedTutorial;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using Spectre.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Utils.CosmosDb.Interop;
using Utils.CosmosDb;

public class Program : IHostedService, IDisposable
{
	// ADD THIS PART TO YOUR CODE

	// The Azure Cosmos DB endpoint for running this sample.
	private static readonly string EndpointUri = "https://localhost:8081";
	// The primary key for the Azure Cosmos account.
	private static readonly string PrimaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

	// The Cosmos client instance
	private CosmosClient cosmosClient;

	// The database we will create
	private Database database;

	// The container we will create.
	private Container container;

	// The name of the database and container we will create
	private string databaseId = "FamilyDatabase";
	private string containerId = "FamilyContainer";

	private Timer timer_ = null;
	private readonly ILogger logger_;
	private readonly IConfiguration configuration_;
	private readonly IServiceProvider provider_;

	public static void Main(string[] args)
	{
		CreateHostBuilder(args)
			.Build()
			.Run()
			;
	}

	public static IHostBuilder CreateHostBuilder(string[] args)
		=> Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((hostContext, app) =>
			{
				var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
				Console.WriteLine("BasePath: " + basePath);
				app.SetBasePath(basePath);
				app.AddJsonFile("host.json", optional: true);
				app.AddJsonFile("appSettings.json", optional: true);
				app.AddEnvironmentVariables(prefix: "DOTNET_");
				app.AddCommandLine(args);
			})
			.ConfigureServices((hostContext, services) =>
			{
				services.AddHostedService<Program>();
				services
					.AddLogging(configure => configure.AddConsole())
					.AddTransient<Program>()
					;

				services.AddTransient<ICosmosRequestChargeOperations, CosmosRequestChargeOperations>();

				services.AddSingleton<CosmosClient>(
					new CosmosClient(EndpointUri, PrimaryKey, GetUnsafeCosmosClientOptions())
				);
			})
			;

	public Program(
		IServiceProvider provider,
		IConfiguration configuration,
		ILogger<Program> logger
	)
	{
		logger_ = logger;
		configuration_ = configuration;
		provider_ = provider;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		timer_ = new Timer(DoWork, null, TimeSpan.FromSeconds(1.0), Timeout.InfiniteTimeSpan);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		timer_?.Change(Timeout.Infinite, 0);
		return Task.CompletedTask;
	}

	private void DoWork(object state)
	{
		Task.Run(async () => await DoWorkAsync());
	}

	public async Task DoWorkAsync()
	{
		try
		{
			logger_.LogDebug("Beginning operations...\n");
			logger_.LogInformation("Information");

			Console.WriteLine($"Setting: {configuration_["setting"]}");
			Console.WriteLine($"LogLevel: {configuration_["logging__logLevel__default"]}");
			var logging = configuration_.GetSection("logging");
			var logLevel = logging.GetSection("logLevel");
			var defaul_ = logLevel.GetValue<string>("default");
			if (defaul_ == null || defaul_.Length == 0)
				Console.WriteLine("No logging");
			Console.WriteLine("level: " + defaul_);

			var operations = provider_.GetService<ICosmosRequestChargeOperations>();
			var database = await operations.CreateDatabaseIfNotExistsAsync(databaseId);
			var container = await operations.CreateContainerIfNotExistsAsync(database, "Profiles", "/id");

			await AddModelAsync(operations, container);

			AnsiConsole.MarkupLine($"[yellow]Request charges: {operations.RequestCharges}RU.[/]");

			logger_.LogDebug("Done.");
		}
		catch (CosmosException de)
		{
			Exception baseException = de.GetBaseException();
			Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
		}
		catch (Exception e)
		{
			Console.WriteLine("Error: {0}", e);
		}
		finally
		{
		}
	}

	/*
		Entry point to call methods that operate on Azure Cosmos DB resources in this sample
	*/
	public async Task GetStartedDemoAsync()
	{
		var operations = provider_.GetService<ICosmosRequestChargeOperations>();

		// Create a new instance of the Cosmos Client
		this.cosmosClient = CreateClient(EndpointUri, PrimaryKey);
		await this.CreateDatabaseAsync();
		this.container = await this.CreateContainerAsync();

		//await this.AddItemsToContainerAsync();
		//await this.QueryItemsAsync();

		var container = await this.AddModelAsync(operations, this.container);

		await this.QueryAddressesAsync(container, "Alice.1");
		await this.QueryAddressesAsync(container, "Bob.1", asc: "desc");
	}

	private async Task<Container> AddModelAsync(ICosmosOperations operations)
	{
		var container = await CreateContainerAsync("Profiles", "/UserId");
		await AddModelAsync(operations, container);
		return container;
	}
	private async Task<Container> AddModelAsync(ICosmosOperations operations, Container container)
	{
		var paths = new[]{
			@"./alice.json",
			@"./bob.json",
		};
		foreach (var path in paths)
		{
			var content = JsonConvert.DeserializeObject<Model.Interop.User>(
				File.ReadAllText(path)
			);

			//foreach (var address in content.Addresses)
			//{
			//	var addr = Models.MaskedEmail.Clone(address);
			//	addr.Id = addr.EmailAddress;
			//	addr.UserId = content.Id;
			//	await operations.CreateItemIfNotExistsAsync(container, addr, addr.UserId);
			//}

			await operations.CreateItemIfNotExistsAsync(container, content, content.Id);
		}
		return container;
	}

	private Task<ItemResponse<T>> InsertItemAsync<T>(T @object, string id, string partition) where T : ICosmosDbItem
	{
		return InsertItemAsync(container, @object, id, partition);
	}
	private async Task<ItemResponse<T>> InsertItemAsync<T>(Container cont, T @object, string id, string partition) where T : ICosmosDbItem
	{

		try
		{
			// Read the item to see if it exists.  
			ItemResponse<T> response = await cont.ReadItemAsync<T>(id, new PartitionKey(partition));
			Console.WriteLine("Item in database with id: {0} already exists\n", response.Resource.Id);
			response = await cont.ReplaceItemAsync<T>(@object, id, new PartitionKey(partition));
			return response;
		}
		catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			// Create an item in the container.
			// Note we provide the value of the partition key for this item
			ItemResponse<T> response = await cont.CreateItemAsync<T>(@object, new PartitionKey(partition));

			// Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse.
			// We can also access the RequestCharge property to see the amount of RUs consumed on this request.
			Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", response.Resource.Id, response.RequestCharge);

			return response;
		}
	}

	/// <summary>
	/// Create the database if it does not exist
	/// </summary>
	private async Task CreateDatabaseAsync()
	{
		// Create a new database
		this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
		Console.WriteLine("Created Database: {0}\n", this.database.Id);
	}

	/// <summary>
	/// Create the container if it does not exist. 
	/// Specifiy "/LastName" as the partition key since we're storing family information, to ensure good distribution of requests and storage.
	/// </summary>
	/// <returns></returns>
	private async Task<Container> CreateContainerAsync(string containerName = null, string partitionPath = "/LastName")
	{
		containerName = containerName ?? this.containerId;
		// Create a new container
		var container = await this.database.CreateContainerIfNotExistsAsync(containerName, partitionPath);
		Console.WriteLine("Created Container: {0}\n", container.Container.Id);
		return container.Container;
	}
	/// <summary>
	/// Add Family items to the container
	/// </summary>
	private async Task AddItemsToContainerAsync()
	{
		// Create a family object for the Andersen family
		Family andersenFamily = new Family
		{
			Id = "Andersen.1",
			LastName = "Andersen",
			Parents = new Parent[]
			{
			new Parent { FirstName = "Thomas" },
			new Parent { FirstName = "Mary Kay" }
			},
			Children = new Child[]
			{
			new Child
			{
				FirstName = "Henriette Thaulow",
				Gender = "female",
				Grade = 5,
				Pets = new Pet[]
				{
					new Pet { GivenName = "Fluffy" }
				}
			}
			},
			Address = new Address { State = "WA", County = "King", City = "Seattle" },
			IsRegistered = false
		};

		await this.InsertItemAsync(andersenFamily, andersenFamily.Id, andersenFamily.LastName);

		Family wakefieldFamily = new Family
		{
			Id = "Wakefield.7",
			LastName = "Wakefield",
			Parents = new Parent[]
			{
			new Parent { FamilyName = "Wakefield", FirstName = "Robin" },
			new Parent { FamilyName = "Miller", FirstName = "Ben" }
			},
			Children = new Child[]
			{
			new Child
			{
				FamilyName = "Merriam",
				FirstName = "Jesse",
				Gender = "female",
				Grade = 8,
				Pets = new Pet[]
				{
					new Pet { GivenName = "Goofy" },
					new Pet { GivenName = "Shadow" }
				}
			},
			new Child
			{
				FamilyName = "Miller",
				FirstName = "Lisa",
				Gender = "female",
				Grade = 1
			}
			},
			Address = new Address { State = "NY", County = "Manhattan", City = "NY" },
			IsRegistered = true
		};

		await this.InsertItemAsync(wakefieldFamily, wakefieldFamily.Id, wakefieldFamily.LastName);

	}
	/// <summary>
	/// Run a query (using Azure Cosmos DB SQL syntax) against the container
	/// </summary>
	private async Task QueryItemsAsync()
	{
		var sqlQueryText = "SELECT * FROM c WHERE c.LastName = 'Andersen'";

		Console.WriteLine("Running query: {0}\n", sqlQueryText);

		QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
		FeedIterator<Family> queryResultSetIterator = this.container.GetItemQueryIterator<Family>(queryDefinition);

		List<Family> families = new List<Family>();

		while (queryResultSetIterator.HasMoreResults)
		{
			FeedResponse<Family> currentResultSet = await queryResultSetIterator.ReadNextAsync();
			foreach (Family family in currentResultSet)
			{
				families.Add(family);
				Console.WriteLine("\tRead {0}\n", family);
			}
		}
	}
	private async Task QueryAddressesAsync(Container container, string partition, int perPage = 1, string sort_by = "c.EmailAddress", string asc = "asc")
	{
		var sqlQueryText = $"SELECT TOP {perPage} * FROM c WHERE c.UserId = '{partition}' ORDER BY {sort_by} {asc}";

		Console.WriteLine("Running query: {0}\n", sqlQueryText);

		QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
		FeedIterator<Models.MaskedEmail> queryResultSetIterator = container.GetItemQueryIterator<Models.MaskedEmail>(queryDefinition);

		List<Models.MaskedEmail> addresses = new List<Models.MaskedEmail>();

		while (queryResultSetIterator.HasMoreResults)
		{
			FeedResponse<Models.MaskedEmail> currentResultSet = await queryResultSetIterator.ReadNextAsync();
			foreach (Models.MaskedEmail address in currentResultSet)
			{
				addresses.Add(address);
				Console.WriteLine("\tRead {0}\n", address);
			}
		}
	}

	private static CosmosClient CreateClient(string endpoint, string primaryKey)
	{
		CosmosClientOptions options = GetUnsafeCosmosClientOptions();
		var client = new CosmosClient(endpoint, primaryKey, options);
		return client;
	}

	private static CosmosClientOptions GetUnsafeCosmosClientOptions()
	{
		return new CosmosClientOptions
		{
			HttpClientFactory = () =>
			{
				HttpMessageHandler httpMessageHandler = new HttpClientHandler
				{
					ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
				};
				return new HttpClient(httpMessageHandler);
			},
			ConnectionMode = ConnectionMode.Gateway,
		};
	}

	public void Dispose()
	{
		timer_?.Dispose();
	}
}