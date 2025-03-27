using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using AsxDbScraper.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Cosmos;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(workerApplication =>
    {
        workerApplication.UseDefaultWorkerMiddleware();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        // Register services
        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));

        services.AddHttpClient();
        services.AddScoped<IAlphaVantageService, AlphaVantageService>();
        services.AddScoped<IAsxCompanyService, AsxCompanyService>();
        services.AddScoped<IAsxScraperService, AsxScraperService>();

        // Configure Cosmos DB
        var connectionString = configuration.GetValue<string>("CosmosDb:ConnectionString")
            ?? throw new InvalidOperationException("Cosmos DB connection string not found in configuration");
        var databaseName = configuration.GetValue<string>("CosmosDb:DatabaseName")
            ?? throw new InvalidOperationException("Cosmos DB database name not found in configuration");

        // Register CosmosClient as a singleton
        services.AddSingleton(sp => new CosmosClient(connectionString));
    })
    .Build();

await host.RunAsync();