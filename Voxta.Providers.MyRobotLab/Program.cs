using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Voxta.Providers.Host;
using Voxta.Providers.MyRobotLab;
using Voxta.Providers.MyRobotLab.Providers;

// Dependency Injection
var services = new ServiceCollection();

// Configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
services.AddSingleton<IConfiguration>(configuration);
services.AddOptions<MyRobotLabOptions>()
    .Bind(configuration.GetSection("MyRobotLab"))
    .ValidateDataAnnotations();

// Logging
services.AddLogging(builder =>
{
    builder.AddConsole();
});

// Dependencies
services.AddHttpClient();
services.AddSingleton<IVoxtaActionsYamlRepository, VoxtaActionsYamlRepository>();

// Voxta Providers
services.AddVoxtaProvider(builder =>
{
    builder.AddProvider<MyRobotLabProvider>();
});

// Build the application
var sp = services.BuildServiceProvider();
var runtime = sp.GetRequiredService<IProviderAppHandler>();

// Run the application
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
await runtime.RunAsync(cts.Token);
