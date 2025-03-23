using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using AsxDbScraper.Services;
using AsxDbScraper.Data;
using Microsoft.EntityFrameworkCore;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(context.Configuration)
            .CreateLogger();

        // Register services
        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));

        services.AddHttpClient();
        services.AddScoped<IAlphaVantageService, AlphaVantageService>();
        services.AddScoped<IAsxCompanyService, AsxCompanyService>();
        services.AddDbContext<AsxDbContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
    })
    .Build();

await host.RunAsync();