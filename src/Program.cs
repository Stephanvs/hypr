using System.CommandLine;
using Hypr.Commands;
using Hypr.Configuration;
using Hypr.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using hypr;

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

// Register services and configuration sections
builder.Services.AddServices(builder.Configuration);

// Automatically discover and register all commands
builder.Services.Scan(s => s.FromAssemblyOf<ListCommand>()
    .AddClasses(c => c.AssignableTo<Command>())
    .As<Command>());

var host = builder.Build();
var rootCommand = new RootCommand("hypr - Git worktree manager");
var commands = host.Services.GetRequiredService<IEnumerable<Command>>();

foreach (var cmd in commands)
{
    rootCommand.Subcommands.Add(cmd);
}

// Parse and invoke
return await rootCommand
    .Parse(args)
    .InvokeAsync();