using System.CommandLine;
using hyprwt;
using Hyprwt.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

var builder = Host.CreateApplicationBuilder(args);
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

builder.Services.AddServices();

// Automatically discover and register all commands
builder.Services.Scan(s => s.FromAssemblyOf<ListCommand>()
    .AddClasses(c => c.AssignableTo<Command>())
    .As<Command>());

var host = builder.Build();
var rootCommand = new RootCommand("hyprwt - Git worktree manager");
var commands = host.Services.GetRequiredService<IEnumerable<Command>>();

foreach (var cmd in commands)
{
    rootCommand.Subcommands.Add(cmd);
}

// Parse and invoke
return await rootCommand
    .Parse(args)
    .InvokeAsync();