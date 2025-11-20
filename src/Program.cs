using System.CommandLine;
using System.CommandLine.Invocation;
using Hyprwt.Commands;
using Hyprwt.Configuration;
using Hyprwt.Hooks;
using Hyprwt.Models;
using Hyprwt.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Hyprwt;

/// <summary>
/// Main entry point for the hyprwt CLI application.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Set up dependency injection
        var services = ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();

        // Create root command
        var rootCommand = BuildRootCommand(serviceProvider);

        // Parse and invoke
        return await rootCommand.InvokeAsync(args);
    }

    private static RootCommand BuildRootCommand(ServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("hyprwt - Git worktree manager")
        {
            Name = "hyprwt"
        };

        // Global options
        var debugOption = new Option<bool>(
            new[] { "--debug" },
            "Enable debug logging");

        var yesOption = new Option<bool>(
            new[] { "-y", "--yes" },
            "Auto-confirm all prompts");

        rootCommand.AddGlobalOption(debugOption);
        rootCommand.AddGlobalOption(yesOption);

        // Default action (no subcommand) - show list
        rootCommand.SetHandler((InvocationContext ctx) =>
        {
            var debug = ctx.ParseResult.GetValueForOption(debugOption);
            SetLogLevel(serviceProvider, debug);
            var listCmd = serviceProvider.GetRequiredService<ListCommand>();
            var exitCode = listCmd.Execute(debug);
            ctx.ExitCode = exitCode;
        });

        // ls command
        var lsCommand = new Command("ls", "List all worktrees");
        lsCommand.SetHandler((InvocationContext ctx) =>
        {
            var debug = ctx.ParseResult.GetValueForOption(debugOption);
            SetLogLevel(serviceProvider, debug);
            var listCmd = serviceProvider.GetRequiredService<ListCommand>();
            var exitCode = listCmd.Execute(debug);
            ctx.ExitCode = exitCode;
        });
        rootCommand.AddCommand(lsCommand);

        // switch command
        var switchCommand = BuildSwitchCommand(serviceProvider, debugOption, yesOption);
        rootCommand.AddCommand(switchCommand);

        // cleanup command
        var cleanupCommand = BuildCleanupCommand(serviceProvider, debugOption, yesOption);
        rootCommand.AddCommand(cleanupCommand);

        // config command
        var configCommand = BuildConfigCommand(serviceProvider, debugOption);
        rootCommand.AddCommand(configCommand);

        return rootCommand;
    }

    private static Command BuildSwitchCommand(ServiceProvider serviceProvider, Option<bool> debugOption, Option<bool> yesOption)
    {
        var switchCommand = new Command("switch", "Switch to or create a worktree for a branch");

        var branchArg = new Argument<string>("branch", "Branch name or path");
        var terminalOption = new Option<string?>("--terminal", "Terminal mode (tab/window/inplace/echo/vscode/cursor)");
        var afterInitOption = new Option<string?>("--after-init", "Command to run after init script");
        var fromOption = new Option<string?>("--from", "Source branch/commit to create from");
        var dirOption = new Option<string?>("--dir", "Custom directory path");

        switchCommand.AddArgument(branchArg);
        switchCommand.AddOption(terminalOption);
        switchCommand.AddOption(afterInitOption);
        switchCommand.AddOption(fromOption);
        switchCommand.AddOption(dirOption);

        switchCommand.SetHandler((InvocationContext ctx) =>
        {
            var debug = ctx.ParseResult.GetValueForOption(debugOption);
            var yes = ctx.ParseResult.GetValueForOption(yesOption);
            SetLogLevel(serviceProvider, debug);

            var branch = ctx.ParseResult.GetValueForArgument(branchArg);
            var terminal = ctx.ParseResult.GetValueForOption(terminalOption);
            var afterInit = ctx.ParseResult.GetValueForOption(afterInitOption);
            var from = ctx.ParseResult.GetValueForOption(fromOption);
            var dir = ctx.ParseResult.GetValueForOption(dirOption);

            if (string.IsNullOrWhiteSpace(branch))
            {
                AnsiConsole.MarkupLine("[yellow]Interactive switch not yet implemented[/]");
                AnsiConsole.MarkupLine("Usage: [cyan]hyprwt switch <branch>[/]");
                ctx.ExitCode = 1;
                return;
            }

            var terminalMode = terminal != null ? ParseTerminalMode(terminal) : (TerminalMode?)null;

            var cmd = new Models.SwitchCommand(
                Branch: branch,
                TerminalMode: terminalMode,
                AfterInit: afterInit,
                AutoConfirm: yes,
                Debug: debug,
                FromBranch: from,
                Dir: dir
            );

            var handler = serviceProvider.GetRequiredService<SwitchCommandHandler>();
            ctx.ExitCode = handler.Execute(cmd);
        });

        return switchCommand;
    }

    private static Command BuildCleanupCommand(ServiceProvider serviceProvider, Option<bool> debugOption, Option<bool> yesOption)
    {
        var cleanupCommand = new Command("cleanup", "Clean up worktrees");

        var modeOption = new Option<string?>("--mode", "Cleanup mode (all/remoteless/merged/interactive/github)");
        var dryRunOption = new Option<bool>("--dry-run", "Show what would be removed");
        var forceOption = new Option<bool>("--force", "Force remove with uncommitted changes");

        cleanupCommand.AddOption(modeOption);
        cleanupCommand.AddOption(dryRunOption);
        cleanupCommand.AddOption(forceOption);

        cleanupCommand.SetHandler(async (InvocationContext ctx) =>
        {
            var mode = ctx.ParseResult.GetValueForOption(modeOption);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOption);
            var force = ctx.ParseResult.GetValueForOption(forceOption);
            var yes = ctx.ParseResult.GetValueForOption(yesOption);
            var debug = ctx.ParseResult.GetValueForOption(debugOption);

            SetLogLevel(serviceProvider, debug);

            var cleanupMode = mode != null ? ParseCleanupMode(mode) : CleanupMode.Interactive;

            var cmd = new Models.CleanupCommand(
                Mode: cleanupMode,
                DryRun: dryRun,
                Force: force,
                AutoConfirm: yes,
                Debug: debug
            );

            var handler = serviceProvider.GetRequiredService<CleanupCommandHandler>();
            ctx.ExitCode = await handler.ExecuteAsync(cmd);
        });

        return cleanupCommand;
    }

    private static Command BuildConfigCommand(ServiceProvider serviceProvider, Option<bool> debugOption)
    {
        var configCommand = new Command("config", "Manage configuration");

        var showOption = new Option<bool>("--show", "Show current configuration");
        configCommand.AddOption(showOption);

        configCommand.SetHandler((InvocationContext ctx) =>
        {
            var show = ctx.ParseResult.GetValueForOption(showOption);
            var debug = ctx.ParseResult.GetValueForOption(debugOption);
            SetLogLevel(serviceProvider, debug);

            var handler = serviceProvider.GetRequiredService<ConfigCommandHandler>();
            ctx.ExitCode = show ? handler.ShowConfig() : handler.EditConfig();
        });

        return configCommand;
    }

    private static TerminalMode ParseTerminalMode(string mode)
    {
        return mode.ToLowerInvariant() switch
        {
            "tab" => TerminalMode.Tab,
            "window" => TerminalMode.Window,
            "inplace" => TerminalMode.Inplace,
            "echo" => TerminalMode.Echo,
            "vscode" => TerminalMode.VSCode,
            "cursor" => TerminalMode.Cursor,
            _ => TerminalMode.Tab
        };
    }

    private static CleanupMode ParseCleanupMode(string mode)
    {
        return mode.ToLowerInvariant() switch
        {
            "all" => CleanupMode.All,
            "remoteless" => CleanupMode.Remoteless,
            "merged" => CleanupMode.Merged,
            "interactive" => CleanupMode.Interactive,
            "github" => CleanupMode.GitHub,
            _ => CleanupMode.Interactive
        };
    }

    private static void SetLogLevel(ServiceProvider serviceProvider, bool debug)
    {
        // This is a simplified version - ideally we'd reconfigure the logger
        // For now, debug logging is set up in ConfigureServices
    }

    private static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Warning);
        });

        // Register services
        services.AddSingleton<StateService>();
        services.AddSingleton<GitService>();
        services.AddSingleton<GitHubService>();
        services.AddSingleton<TerminalService>();
        services.AddSingleton<VersionCheckService>();
        services.AddSingleton<ConfigLoader>();
        services.AddSingleton<HookRunner>();

        // Register command handlers
        services.AddSingleton<ListCommand>();
        services.AddSingleton<SwitchCommandHandler>();
        services.AddSingleton<CleanupCommandHandler>();
        services.AddSingleton<ConfigCommandHandler>();

        return services;
    }
}
