using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(Startup.ConfigureServices)
    .Build();

await host.RunAsync(); 