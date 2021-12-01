using System;
using System.Threading.Tasks;
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
using Microsoft.Extensions.Configuration;
using Utils.CosmosDb.Interop;
using Utils.CosmosDb;

public class Program
{
    // The Cosmos client instance
    private CosmosClient cosmosClient;

    // The database we will create
    private Database database;

    // The container we will create.
    private Container container;

    // The name of the database and container we will create
    private string databaseId = "FamilyDatabase";
    private string containerId = "FamilyContainer";

    private readonly AppSettings appSettings_;

    private readonly ILogger logger_;
    private readonly ICosmosRequestChargeOperations operations_;

    public async static Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await host
            .Services
            .GetRequiredService<Program>()
            .RunAsync(args)
            ;
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configHost =>
            {
                var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);

                configHost.SetBasePath(basePath);

                configHost
                    .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                    .AddJsonFile("host.json", optional: true)
                    ;
            })
            .ConfigureAppConfiguration((hostContext, app) =>
            {
                IHostEnvironment env = hostContext.HostingEnvironment;
                AnsiConsole.MarkupLine($"[yellow]Environment: {env.EnvironmentName}.[/]");

                var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);

                app.SetBasePath(basePath);

                app
                    .AddJsonFile("appSettings.json", optional: true)
                    .AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: true)
                    .AddCommandLine(args)
                    ;

                //if (env.IsDevelopment())
                //{
                //    app.AddUserSecrets<Program>(optional: true);
                //}
            })
            .ConfigureServices((hostContext, services) =>
            {
                var appSettings = GetAppSettings(hostContext);
                var endpointUri = appSettings.CosmosDb.EndpointUri;
                var accessKey = appSettings.CosmosDb.PrimaryKey;
                var skipSslValidation = appSettings.CosmosDb.IgnoreSslServerCertificateValidation;

                AnsiConsole.MarkupLine($"[yellow]CosmosDb Endpoint: {endpointUri}.[/]");
                AnsiConsole.MarkupLine($"[yellow]CosmosDb Primary Key: {accessKey.Substring(0, 4)}***REDACTED***.[/]");
                AnsiConsole.MarkupLine($"[yellow]Ignore CosmosDb Ssl Server Certificate: {skipSslValidation}.[/]");

                services.AddLogging(configure => configure.AddConsole());

                services.AddTransient<ICosmosRequestChargeOperations, CosmosRequestChargeOperations>();
                services.AddTransient<Program>();

                services.AddSingleton<AppSettings>(GetAppSettings(hostContext));
                services.AddSingleton<CosmosClient>(
                    new CosmosClient(
                        endpointUri,
                        accessKey,
                        skipSslValidation
                        ? GetUnsafeCosmosClientOptions()
                        : new CosmosClientOptions()
                        )
                );
            });
    }
    private static AppSettings GetAppSettings(HostBuilderContext hostContext)
    {
        var appSettings = new AppSettings()
        {
            CosmosDb = new AppSettings.CosmosDbSettings(),
        };
        var cosmosDbSection = hostContext.Configuration.GetSection("CosmosDb");
        cosmosDbSection.Bind(appSettings.CosmosDb);
        return appSettings;
    }

    public Program(
        AppSettings appSettings,
        ICosmosRequestChargeOperations operations,
        ILogger<Program> logger
    )
    {
        appSettings_ = appSettings;

        logger_ = logger;
        operations_ = operations;
    }

    public async Task RunAsync(string[] args)
    {
        try
        {
            logger_.LogDebug("Beginning operations...\n");

            var database = await operations_.CreateDatabaseIfNotExistsAsync(databaseId);
            var container = await operations_.CreateContainerIfNotExistsAsync(database, "Profiles", "/id");

            await AddModelAsync(operations_, container);

            AnsiConsole.MarkupLine($"[yellow]Request charges: {operations_.RequestCharges}RU.[/]");

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
        // Create a new instance of the Cosmos Client
        this.cosmosClient = CreateClient(appSettings_.CosmosDb.EndpointUri, appSettings_.CosmosDb.PrimaryKey);
        await this.CreateDatabaseAsync();
        this.container = await this.CreateContainerAsync();

        //await this.AddItemsToContainerAsync();
        //await this.QueryItemsAsync();

        var container = await this.AddModelAsync(operations_, this.container);

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
                    ServerCertificateCustomValidationCallback = (req, cert, chain, errs) =>
                    {
                        AnsiConsole.MarkupLine("[Red]Ignoring untrusted Ssl server certificate[/].");
                        return HttpClientHandler.DangerousAcceptAnyServerCertificateValidator(req, cert, chain, errs);
                    }
                };
                return new HttpClient(httpMessageHandler);
            },
            ConnectionMode = ConnectionMode.Gateway,
        };
    }
}