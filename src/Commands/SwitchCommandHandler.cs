using Hyprwt.Configuration;
using Hyprwt.Hooks;
using Hyprwt.Models;
using Hyprwt.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Hyprwt.Commands;

/// <summary>
/// Switches to or creates a worktree for a branch.
/// </summary>
public class SwitchCommandHandler
{
    private readonly ILogger<SwitchCommandHandler> _logger;
    private readonly GitService _gitService;
    private readonly TerminalService _terminalService;
    private readonly ConfigLoader _configLoader;
    private readonly HookRunner _hookRunner;

    public SwitchCommandHandler(
        ILogger<SwitchCommandHandler> logger,
        GitService gitService,
        TerminalService terminalService,
        ConfigLoader configLoader,
        HookRunner hookRunner)
    {
        _logger = logger;
        _gitService = gitService;
        _terminalService = terminalService;
        _configLoader = configLoader;
        _hookRunner = hookRunner;
    }

    /// <summary>
    /// Executes the switch command.
    /// </summary>
    public int Execute(Models.SwitchCommand command)
    {
        try
        {
            var repoPath = _gitService.FindRepoRoot();
            if (repoPath == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Not in a git repository");
                return 1;
            }

            // Load configuration
            var config = _configLoader.LoadConfig(repoPath);

            // Check if worktree already exists
            var worktrees = _gitService.ListWorktrees(repoPath);
            var existingWorktree = worktrees.FirstOrDefault(w => w.Branch == command.Branch);

            if (existingWorktree != null)
            {
                // Switch to existing worktree
                AnsiConsole.MarkupLine($"[green]Switching to existing worktree:[/] {command.Branch}");

                var terminalMode = command.TerminalMode ?? config.Terminal.Mode;
                var success = _terminalService.OpenWorktree(
                    existingWorktree.Path,
                    terminalMode,
                    command.InitScript ?? config.Scripts.SessionInit,
                    command.AfterInit
                );

                return success ? 0 : 1;
            }

            // Create new worktree
            AnsiConsole.MarkupLine($"[yellow]Creating new worktree for branch:[/] {command.Branch}");

            // Generate worktree path
            var worktreePath = GenerateWorktreePath(repoPath, command.Branch, command.Dir, config);

            // Run pre-create hook
            if (config.Scripts.PreCreate != null)
            {
                AnsiConsole.MarkupLine("[dim]Running pre-create hook...[/]");
                _hookRunner.RunHook(config.Scripts.PreCreate, repoPath);
            }

            // Confirm creation if not auto-confirmed
            if (!command.AutoConfirm)
            {
                if (!AnsiConsole.Confirm($"Create worktree at {worktreePath}?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                    return 0;
                }
            }

            // Create the worktree
            var created = _gitService.CreateWorktree(
                repoPath,
                worktreePath,
                command.Branch,
                command.FromBranch
            );

            if (!created)
            {
                AnsiConsole.MarkupLine("[red]Failed to create worktree[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] Created worktree at {worktreePath}");

            // Run post-create hook (synchronous)
            if (config.Scripts.PostCreate != null)
            {
                AnsiConsole.MarkupLine("[dim]Running post-create hook...[/]");
                _hookRunner.RunHook(config.Scripts.PostCreate, worktreePath);
            }

            // Run post-create-async hook (fire and forget)
            if (config.Scripts.PostCreateAsync != null)
            {
                _hookRunner.RunHook(config.Scripts.PostCreateAsync, worktreePath, async: true);
            }

            // Open in terminal
            var termMode = command.TerminalMode ?? config.Terminal.Mode;
            var opened = _terminalService.OpenWorktree(
                worktreePath,
                termMode,
                command.InitScript ?? config.Scripts.SessionInit,
                command.AfterInit
            );

            return opened ? 0 : 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to worktree");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static string GenerateWorktreePath(string repoPath, string branch, string? customDir, Config config)
    {
        if (customDir != null)
            return Path.GetFullPath(customDir);

        // Use directory pattern from config
        var pattern = config.Worktree.DirectoryPattern;
        var repoName = Path.GetFileName(repoPath);

        // Replace placeholders
        var path = pattern
            .Replace("{repo_name}", repoName)
            .Replace("{branch}", branch);

        // Make it absolute relative to repo
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(repoPath, path);
        }

        return Path.GetFullPath(path);
    }
}
