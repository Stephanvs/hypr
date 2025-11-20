using Hyprwt.Configuration;
using Hyprwt.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Hyprwt.Commands;

/// <summary>
/// Manages configuration settings.
/// </summary>
public class ConfigCommandHandler
{
    private readonly ILogger<ConfigCommandHandler> _logger;
    private readonly ConfigLoader _configLoader;

    public ConfigCommandHandler(ILogger<ConfigCommandHandler> logger, ConfigLoader configLoader)
    {
        _logger = logger;
        _configLoader = configLoader;
    }

    /// <summary>
    /// Shows current configuration.
    /// </summary>
    public int ShowConfig()
    {
        try
        {
            var config = _configLoader.LoadGlobalConfigOnly();

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("[bold]Setting[/]");
            table.AddColumn("[bold]Value[/]");

            // Terminal settings
            table.AddRow("[cyan]terminal.mode[/]", config.Terminal.Mode.ToString().ToLower());
            table.AddRow("[cyan]terminal.always_new[/]", config.Terminal.AlwaysNew.ToString().ToLower());
            if (config.Terminal.Program != null)
                table.AddRow("[cyan]terminal.program[/]", config.Terminal.Program);

            // Worktree settings
            table.AddRow("[cyan]worktree.directory_pattern[/]", config.Worktree.DirectoryPattern);
            table.AddRow("[cyan]worktree.auto_fetch[/]", config.Worktree.AutoFetch.ToString().ToLower());
            if (config.Worktree.BranchPrefix != null)
                table.AddRow("[cyan]worktree.branch_prefix[/]", config.Worktree.BranchPrefix);

            // Cleanup settings
            table.AddRow("[cyan]cleanup.default_mode[/]", config.Cleanup.DefaultMode.ToString().ToLower());

            // Scripts (if any)
            if (config.Scripts.SessionInit != null)
                table.AddRow("[cyan]scripts.session_init[/]", config.Scripts.SessionInit);
            if (config.Scripts.PostCreate != null)
                table.AddRow("[cyan]scripts.post_create[/]", config.Scripts.PostCreate);

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

    /// <summary>
    /// Interactive configuration editor (simplified for now).
    /// </summary>
    public int EditConfig()
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
