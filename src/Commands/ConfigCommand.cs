using System.CommandLine;
using Hypr.Configuration;
using Hypr.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace Hypr.Commands;

/// <summary>
/// Manages configuration settings.
/// </summary>
public class ConfigCommand : Command
{
    private readonly ILogger<ConfigCommand> _logger;
    private readonly IOptionsMonitor<TerminalConfig> _terminalConfig;
    private readonly IOptionsMonitor<WorktreeConfig> _worktreeConfig;
    private readonly IOptionsMonitor<CleanupConfig> _cleanupConfig;
    private readonly IOptionsMonitor<ScriptsConfig> _scriptsConfig;

    public ConfigCommand(
        ILogger<ConfigCommand> logger,
        IOptionsMonitor<TerminalConfig> terminalConfig,
        IOptionsMonitor<WorktreeConfig> worktreeConfig,
        IOptionsMonitor<CleanupConfig> cleanupConfig,
        IOptionsMonitor<ScriptsConfig> scriptsConfig)
        : base("config", "Manage configuration settings")
    {
        _logger = logger;
        _terminalConfig = terminalConfig;
        _worktreeConfig = worktreeConfig;
        _cleanupConfig = cleanupConfig;
        _scriptsConfig = scriptsConfig;

        Aliases.Add("cfg");
        Aliases.Add("conf");

        Add(ShowOption);
        Add(EditOption);

        SetAction(Execute);
    }

    private Option<bool> ShowOption { get; } =
        new("--show")
        {
            Description = "Show current configuration",
        };

    private Option<bool> EditOption { get; } =
        new("--edit")
        {
            Description = "Edit configuration",
        };

    private int Execute(ParseResult ctx)
    {
        try
        {
            var show = ctx.GetValue(ShowOption);
            var edit = ctx.GetValue(EditOption);

            // Default to showing config if no option is specified
            if (!show && !edit)
            {
                show = true;
            }

            if (edit)
            {
                return EditConfig();
            }
            else
            {
                return ShowConfig();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute config command");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private int ShowConfig()
    {
        try
        {
            var terminal = _terminalConfig.CurrentValue;
            var worktree = _worktreeConfig.CurrentValue;
            var cleanup = _cleanupConfig.CurrentValue;
            var scripts = _scriptsConfig.CurrentValue;

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("[bold]Setting[/]");
            table.AddColumn("[bold]Value[/]");

            // Terminal settings
            table.AddRow("[cyan]terminal.mode[/]", terminal.Mode.ToString().ToLower());
            table.AddRow("[cyan]terminal.always_new[/]", terminal.AlwaysNew.ToString().ToLower());
            if (terminal.Program != null)
                table.AddRow("[cyan]terminal.program[/]", terminal.Program);

            // Worktree settings
            table.AddRow("[cyan]worktree.directory_pattern[/]", worktree.DirectoryPattern);
            table.AddRow("[cyan]worktree.auto_fetch[/]", worktree.AutoFetch.ToString().ToLower());
            if (worktree.BranchPrefix != null)
                table.AddRow("[cyan]worktree.branch_prefix[/]", worktree.BranchPrefix);

            // Cleanup settings
            table.AddRow("[cyan]cleanup.default_mode[/]", cleanup.DefaultMode.ToString().ToLower());

            // Scripts (if any)
            if (scripts.SessionInit != null)
                table.AddRow("[cyan]scripts.session_init[/]", scripts.SessionInit);
            if (scripts.PostCreate != null)
                table.AddRow("[cyan]scripts.post_create[/]", scripts.PostCreate);

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show configuration");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private int EditConfig()
    {
        try
        {
            AnsiConsole.MarkupLine("[yellow]Interactive config editor not yet implemented[/]");
            AnsiConsole.MarkupLine("\nUse [cyan]hyprwt config --show[/] to view current settings");

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit configuration");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
