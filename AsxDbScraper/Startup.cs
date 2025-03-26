using AsxDbScraper.Services;
using AsxDbScraper.Data;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Logging;

namespace AsxDbScraper;

public class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Create logs directory if it doesn't exist
        var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsPath);

        // Configure Serilog with cross-platform paths
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logsPath, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // Add Serilog to the service collection
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Register services
        services.AddHttpClient();
        services.AddScoped<IAsxScraperService, AsxScraperService>();
        services.AddScoped<IAlphaVantageService, AlphaVantageService>();
        services.AddScoped<IAsxCompanyService, AsxCompanyService>();
        services.AddDbContext<AsxDbContext>();
    }
} 