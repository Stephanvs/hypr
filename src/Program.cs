using System.CommandLine;
using hypr;
using Hypr.Commands;
using Hypr.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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

builder.Services.AddLogging(x =>
{
    x.ClearProviders();
    x.AddSimpleConsole(f =>
    {
        f.SingleLine = true;
        f.TimestampFormat = "HH:mm:ss ";
        f.IncludeScopes = true;
        f.ColorBehavior = LoggerColorBehavior.Enabled;
    });
});

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
