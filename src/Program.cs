using System.CommandLine;
using Hypr.Configuration;
using Hypr.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hypr;

// Build configuration with proper precedence order
var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile(PathProvider.GetGlobalConfigPath(), optional: true, reloadOnChange: true)
    .AddJsonFile("hypr.json", optional: true, reloadOnChange: true)
    .AddJsonFile(".hypr.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("HYPR_");

var configuration = configBuilder.Build();

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddConfiguration(configuration);

// Add Hypr logging with debug flag support
builder.Services.AddHyprLogging(args);

// Register configuration
builder.Services.AddSingleton<IConfiguration>(configuration);

// Register services, configuration sections, and commands
builder.Services.AddServices(builder.Configuration);

var host = builder.Build();
var rootCommand = new RootCommand("hypr - Git worktree manager");
rootCommand.Options.Add(new DebugOption());
var commands = host.Services.GetRequiredService<IEnumerable<Command>>();

foreach (var cmd in commands)
{
    rootCommand.Subcommands.Add(cmd);
}

// Parse and invoke
return await rootCommand
    .Parse(args)
    .InvokeAsync();
