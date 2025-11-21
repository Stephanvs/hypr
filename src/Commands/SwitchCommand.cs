using System.CommandLine;
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
public class SwitchCommand : Command
{
    private readonly ILogger<SwitchCommand> _logger;
    private readonly GitService _gitService;
    private readonly TerminalService _terminalService;
    private readonly ConfigLoader _configLoader;
    private readonly HookRunner _hookRunner;

    private Argument<string> BranchArgument { get; } =
        new("branch")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "Branch name or path",
        };

    private Option<TerminalMode?> TerminalModeOption { get; } =
        new("--terminal")
        {
            Aliases = { "-tm", "--term" },
            Description = "Terminal mode",
            DefaultValueFactory = _ => TerminalMode.Tab,
        };

    private Option<string?> AfterInitOption { get; } =
        new("--after-init")
        {
            Aliases = { "-ai" },
            Description = "Command to run after init script",
        };

    private Option<string?> FromBranchOption { get; } =
        new("--from")
        {
            Aliases = { "-fb", "--from-branch" },
            Description = "Source branch/commit to create from"
        };

    private Option<DirectoryInfo?> DirOption { get; } =
        new("--dir")
        {
            Description = "Custom directory path",
        };

    private AutoConfirmOption AutoConfirmOption { get; } = new();

    public SwitchCommand(
        ILogger<SwitchCommand> logger,
        GitService gitService,
        TerminalService terminalService,
        ConfigLoader configLoader,
        HookRunner hookRunner)
        : base("switch", "Switch to or create a worktree for a branch")
    {
        _logger = logger;
        _gitService = gitService;
        _terminalService = terminalService;
        _configLoader = configLoader;
        _hookRunner = hookRunner;

        Aliases.Add("sw");
        Aliases.Add("checkout");
        Aliases.Add("co");
        Aliases.Add("goto");
        Aliases.Add("go");

        Add(BranchArgument);
        Add(FromBranchOption);
        Add(TerminalModeOption);
        Add(AfterInitOption);
        Add(DirOption);
        Add(AutoConfirmOption);

        SetAction(Execute);
    }

    private int Execute(ParseResult ctx)
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

            var branch = ctx.GetRequiredValue(BranchArgument);
            var fromBranch = ctx.GetValue(FromBranchOption);
            var terminalMode = ctx.GetValue(TerminalModeOption) ?? config.Terminal.Mode;
            var initScript = ctx.GetValue(AfterInitOption) ?? config.Scripts.SessionInit;
            var afterInit = ctx.GetValue(AfterInitOption);
            var dir = ctx.GetValue(DirOption);

            // Check if worktree already exists
            var worktrees = _gitService.ListWorktrees(repoPath);
            var existingWorktree = worktrees.FirstOrDefault(w => w.Branch == branch);

            if (existingWorktree != null)
            {
                // Switch to existing worktree
                AnsiConsole.MarkupLine($"[green]Switching to existing worktree:[/] {branch}");

                var success = _terminalService.OpenWorktree(
                    existingWorktree.Path,
                    terminalMode,
                    initScript,
                    afterInit
                );

                return success ? 0 : 1;
            }

            // Create new worktree
            AnsiConsole.MarkupLine($"[yellow]Creating new worktree for branch:[/] {branch}");

            // Generate worktree path
            var worktreePath = GenerateWorktreePath(repoPath, branch, dir, config);

            // Run pre-create hook
            if (config.Scripts.PreCreate != null)
            {
                AnsiConsole.MarkupLine("[dim]Running pre-create hook...[/]");
                _hookRunner.RunHook(config.Scripts.PreCreate, repoPath);
            }

            // Confirm creation if not auto-confirmed
            var autoConfirm = ctx.GetValue(AutoConfirmOption);
            if (!autoConfirm)
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
                branch,
                fromBranch
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
            var opened = _terminalService.OpenWorktree(
                worktreePath,
                terminalMode,
                initScript,
                afterInit
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

    private static string GenerateWorktreePath(string repoPath, string branch, DirectoryInfo? customDir, Config config)
    {
        if (customDir is not null)
            return customDir.FullName;

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
