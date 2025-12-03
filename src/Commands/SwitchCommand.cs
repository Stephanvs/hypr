using System.CommandLine;
using Hypr.Configuration;
using Hypr.Hooks;
using Hypr.Models;
using Hypr.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace Hypr.Commands;

/// <summary>
/// Switches to or creates a worktree for a branch.
/// </summary>
public class SwitchCommand : Command
{
    private readonly ILogger<SwitchCommand> _logger;
    private readonly GitService _gitService;
    private readonly TerminalService _terminalService;
    private readonly IOptionsMonitor<TerminalConfig> _terminalConfig;
    private readonly IOptionsMonitor<ScriptsConfig> _scriptsConfig;
    private readonly IOptionsMonitor<WorktreeConfig> _worktreeConfig;
    private readonly HookRunner _hookRunner;

    private Argument<string> BranchArgument { get; } =
        new("branch")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "Branch name or path",
        };

    private Option<TerminalMode?> TerminalModeOption { get; }

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
        IOptionsMonitor<TerminalConfig> terminalConfig,
        IOptionsMonitor<ScriptsConfig> scriptsConfig,
        IOptionsMonitor<WorktreeConfig> worktreeConfig,
        HookRunner hookRunner)
        : base("switch", "Switch to or create a worktree for a branch")
    {
        _logger = logger;
        _gitService = gitService;
        _terminalService = terminalService;
        _terminalConfig = terminalConfig;
        _scriptsConfig = scriptsConfig;
        _worktreeConfig = worktreeConfig;
        _hookRunner = hookRunner;

        Aliases.Add("sw");
        Aliases.Add("checkout");
        Aliases.Add("co");
        Aliases.Add("goto");
        Aliases.Add("go");

        TerminalModeOption = new("--terminal")
        {
            Aliases = { "-tm", "--term" },
            Description = "Terminal mode",
            DefaultValueFactory = _ => _terminalConfig.CurrentValue.Mode,
        };

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

            var terminalConfig = _terminalConfig.CurrentValue;
            var scriptsConfig = _scriptsConfig.CurrentValue;
            var worktreeConfig = _worktreeConfig.CurrentValue;

            var branch = ctx.GetRequiredValue(BranchArgument);
            var fromBranch = ctx.GetValue(FromBranchOption);
            var terminalMode = ctx.GetValue(TerminalModeOption) ?? terminalConfig.Mode;
            var initScript = ctx.GetValue(AfterInitOption) ?? scriptsConfig.SessionInit;
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
            var worktreePath = GenerateWorktreePath(repoPath, branch, dir, worktreeConfig);

            // Run pre-create hook
            if (!string.IsNullOrEmpty(scriptsConfig.PreCreate))
            {
                AnsiConsole.MarkupLine("[dim]Running pre-create hook...[/]");
                _hookRunner.RunHook(scriptsConfig.PreCreate, repoPath);
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
            if (!string.IsNullOrEmpty(scriptsConfig.PostCreate))
            {
                AnsiConsole.MarkupLine("[dim]Running post-create hook...[/]");
                _hookRunner.RunHook(scriptsConfig.PostCreate, worktreePath);
            }

            // Run post-create-async hook (fire and forget)
            if (!string.IsNullOrEmpty(scriptsConfig.PostCreateAsync))
            {
                _hookRunner.RunHook(scriptsConfig.PostCreateAsync, worktreePath, async: true);
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

    private static string GenerateWorktreePath(string repoPath, string branch, DirectoryInfo? customDir, WorktreeConfig config)
    {
        if (customDir is not null)
            return customDir.FullName;

        // Use directory pattern from config
        var pattern = config.DirectoryPattern;
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
