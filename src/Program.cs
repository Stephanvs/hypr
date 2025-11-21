using System.CommandLine;
using Hyprwt.Commands;
using Hyprwt.Configuration;
using Hyprwt.Hooks;
using Hyprwt.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Hyprwt;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
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

        ConfigureServices(builder.Services);

        // Automatically discover and register all commands
        builder.Services.Scan(s => s.FromAssemblyOf<ListCommand>()
            .AddClasses(c => c.AssignableTo<Command>())
            .As<Command>());

        var host = builder.Build();

        // Create root command
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
    }

    // private static RootCommand BuildRootCommand(ServiceProvider serviceProvider)
    // {
    //     Global options
    //     var debugOption = new DebugOption();
    //     var yesOption = new YesOption();
    //
    //     rootCommand.AddGlobalOption(debugOption);
    //     rootCommand.AddGlobalOption(yesOption);
    //
    //     switch command
    //     var switchCommand = BuildSwitchCommand(serviceProvider, debugOption, yesOption);
    //
    //     config command
    //     var configCommand = BuildConfigCommand(serviceProvider, debugOption);
    // }

    private static Command BuildSwitchCommand(ServiceProvider serviceProvider, Option<bool> debugOption, Option<bool> yesOption)
    {
        var switchCommand = new Command("switch", "Switch to or create a worktree for a branch");

        // var branchArg = new Argument<string>("branch", "Branch name or path");
        // var terminalOption = new Option<TerminalMode>("--terminal", "Terminal mode");
        // var afterInitOption = new Option<string?>("--after-init", "Command to run after init script");
        // var fromOption = new Option<string?>("--from", "Source branch/commit to create from");
        // var dirOption = new Option<string?>("--dir", "Custom directory path");
        //
        // switchCommand.AddArgument(branchArg);
        // switchCommand.AddOption(terminalOption);
        // switchCommand.AddOption(afterInitOption);
        // switchCommand.AddOption(fromOption);
        // switchCommand.AddOption(dirOption);
        //
        // switchCommand.SetHandler((InvocationContext ctx) =>
        // {
        //     var debug = ctx.ParseResult.GetValueForOption(debugOption);
        //     var yes = ctx.ParseResult.GetValueForOption(yesOption);
        //     SetLogLevel(serviceProvider, debug);
        //
        //     var branch = ctx.ParseResult.GetValueForArgument(branchArg);
        //     var terminal = ctx.ParseResult.GetValueForOption(terminalOption);
        //     var afterInit = ctx.ParseResult.GetValueForOption(afterInitOption);
        //     var from = ctx.ParseResult.GetValueForOption(fromOption);
        //     var dir = ctx.ParseResult.GetValueForOption(dirOption);
        //
        //     if (string.IsNullOrWhiteSpace(branch))
        //     {
        //         AnsiConsole.MarkupLine("[yellow]Interactive switch not yet implemented[/]");
        //         AnsiConsole.MarkupLine("Usage: [cyan]hyprwt switch <branch>[/]");
        //         ctx.ExitCode = 1;
        //         return;
        //     }
        //
        //     var cmd = new SwitchCommand(
        //         Branch: branch,
        //         TerminalMode: terminal,
        //         AfterInit: afterInit,
        //         AutoConfirm: yes,
        //         Debug: debug,
        //         FromBranch: from,
        //         Dir: dir
        //     );
        //
        //     var handler = serviceProvider.GetRequiredService<SwitchCommandHandler>();
        //     ctx.ExitCode = handler.Execute(cmd);
        // });

        return switchCommand;
    }

    private static Command BuildConfigCommand(ServiceProvider serviceProvider, Option<bool> debugOption)
    {
        var configCommand = new Command("config", "Manage configuration");

        var showOption = new Option<bool>("--show", "Show current configuration");
        // configCommand.AddOption(showOption);

        // configCommand.SetHandler((InvocationContext ctx) =>
        // {
        //     var show = ctx.ParseResult.GetValueForOption(showOption);
        //     var debug = ctx.ParseResult.GetValueForOption(debugOption);
        //     SetLogLevel(serviceProvider, debug);
        //
        //     var handler = serviceProvider.GetRequiredService<ConfigCommandHandler>();
        //     ctx.ExitCode = show ? handler.ShowConfig() : handler.EditConfig();
        // });

        return configCommand;
    }

    private static IServiceCollection ConfigureServices(
        IServiceCollection services)
        // Register services
        => services
            .AddSingleton<StateService>()
            .AddSingleton<GitService>()
            .AddSingleton<GitHubService>()
            .AddSingleton<TerminalService>()
            .AddSingleton<VersionCheckService>()
            .AddSingleton<ConfigLoader>()
            .AddSingleton<HookRunner>();
}
