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

    const string DatabaseId = "ProfilesDb";
    const string PartitionKeyPath = "/id";

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
				AnsiConsole.MarkupLine($"[grey]Environment: {env.EnvironmentName}.[/]");

				var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);

				app.SetBasePath(basePath);

				app
					.AddJsonFile("appSettings.json", optional: true)
					.AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: true)
					.AddCommandLine(args)
					;

				if (env.IsDevelopment() && (args.Length == 0 || args[0] != "--no-secrets"))
				{
					app.AddUserSecrets<Program>(optional: true);
				}
			})
			.ConfigureServices((hostContext, services) =>
			{
				var appSettings = GetAppSettings(hostContext);
				var endpointUri = appSettings.CosmosDb.EndpointUri;
				var accessKey = appSettings.CosmosDb.PrimaryKey;
				var skipSslValidation = appSettings.CosmosDb.IgnoreSslServerCertificateValidation;

				AnsiConsole.MarkupLine($"[grey]CosmosDb Endpoint: {endpointUri}.[/]");
				AnsiConsole.MarkupLine($"[grey]CosmosDb Primary Key: {accessKey.Substring(0, 4)}***REDACTED***.[/]");
				AnsiConsole.MarkupLine($"[grey]CosmosDb Ignore Ssl Server Certificate: {skipSslValidation}.[/]");

				services.AddLogging(configure => configure.AddConsole());

				services.AddTransient<ICosmosOperations, CosmosOperations>(provider =>
				{
					var logger = provider.GetRequiredService<ILogger<CosmosOperations>>();
					var client = new CosmosClient(
						endpointUri,
						accessKey,
						skipSslValidation
						? GetUnsafeCosmosClientOptions()
						: new CosmosClientOptions()
						);

					return new CosmosOperations(client, logger);
				});

				services.AddTransient<ICosmosRequestChargeOperations, CosmosRequestChargeOperations>();
				services.AddTransient<Program>();

				services.AddSingleton<AppSettings>(GetAppSettings(hostContext));
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

			Database database = await operations_.CreateDatabaseIfNotExistsAsync(databaseId);
			Container container = await operations_.CreateContainerIfNotExistsAsync(database, DatabaseId, PartitionKeyPath);

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

	private static bool unsafeOptionsWarningDisplayed = false;
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
						if (!unsafeOptionsWarningDisplayed)
						{
							AnsiConsole.MarkupLine("[yellow]Ignoring untrusted Ssl server certificate[/].");
							unsafeOptionsWarningDisplayed = true;
						}
						return HttpClientHandler.DangerousAcceptAnyServerCertificateValidator(req, cert, chain, errs);
					}
				};
				return new HttpClient(httpMessageHandler);
			},
			ConnectionMode = ConnectionMode.Gateway,
		};
	}
}