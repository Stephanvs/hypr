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

// Check for debug flag in environment or args
var isDebug = args.Contains("--debug") || 
    (Environment.GetEnvironmentVariable("HYPR_DEBUG")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(isDebug ? LogEventLevel.Debug : LogEventLevel.Information)
    .WriteTo.File(PathProvider.GetLogFilePath(), 
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .WriteTo.Console(
        outputTemplate: isDebug 
            ? "{Message:lj}{NewLine}{Exception}" 
            : "[{Level:u3}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: isDebug ? LogEventLevel.Debug : LogEventLevel.Warning)
    .CreateLogger();

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

builder.Services.AddLogging(x => x.ClearProviders().AddSerilog());

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
