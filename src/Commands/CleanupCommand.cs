using System.CommandLine;
using Hyprwt.Configuration;
using Hyprwt.Models;
using Hyprwt.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Hyprwt.Commands;

/// <summary>
/// Cleans up worktrees based on various criteria.
/// </summary>
public class CleanupCommand : Command
{
    private readonly ILogger<CleanupCommand> _logger;
    private readonly GitService _gitService;
    private readonly ConfigLoader _configLoader;

    /// <summary>
    /// Cleans up worktrees based on various criteria.
    /// </summary>
    public CleanupCommand(
        ILogger<CleanupCommand> logger,
        GitService gitService,
        ConfigLoader configLoader)
        : base("cleanup", "Clean up worktrees")
    {
        _logger = logger;
        _gitService = gitService;
        _configLoader = configLoader;

        Aliases.Add("cl");
        Aliases.Add("clean");
        Aliases.Add("prune");
        Aliases.Add("rm");
        Aliases.Add("remove");
        Aliases.Add("del");
        Aliases.Add("delete");

        Add(CleanupMode);
        Add(DryRun);
        Add(Force);
        Add(new AutoConfirmOption());

        SetAction(ExecuteAsync);
    }

    private Option<CleanupMode> CleanupMode { get; } = new("--mode")
    {
        Description = "Determine which worktrees to remove",
        DefaultValueFactory = (_ => Models.CleanupMode.All)
    };

    private Option<bool> DryRun { get; } = new("--dry-run")
    {
        Description = "Show what would be removed, but don't actually remove anything"
    };

    private Option<bool> Force { get; } = new("--force")
    {
        Description = "Force removal of non-empty worktrees"
    };

    private async Task<int> ExecuteAsync(ParseResult ctx)
    {
        var cleanMode = ctx.GetValue(CleanupMode);
        var dryRun = ctx.GetValue(DryRun);
        var force = ctx.GetValue(Force);
        var autoConfirm = ctx.GetValue(new AutoConfirmOption());

        try
        {
            var repoPath = _gitService.FindRepoRoot();
            if (repoPath == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Not in a git repository");
                return 1;
            }

            _configLoader.LoadConfig(repoPath);
            var worktrees = _gitService.ListWorktrees(repoPath);

            // Exclude primary worktree
            var candidates = worktrees.Where(w => !w.IsPrimary).ToList();

            if (candidates.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No worktrees to clean up[/]");
                return 0;
            }

            var toRemove = await FilterWorktreesAsync(repoPath, candidates, cleanMode);

            if (toRemove.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No worktrees match cleanup criteria[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[yellow]Found {toRemove.Count} worktree(s) to remove:[/]");
            foreach (var wt in toRemove)
            {
                AnsiConsole.MarkupLine($"  • [cyan]{wt.Branch}[/] ({wt.Path})");
            }

            if (dryRun)
            {
                AnsiConsole.MarkupLine("[dim]Dry run - no changes made[/]");
                return 0;
            }

            if (!autoConfirm)
            {
                if (!AnsiConsole.Confirm($"Remove {toRemove.Count} worktree(s)?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                    return 0;
                }
            }

            var removed = 0;
            foreach (var wt in toRemove)
            {
                AnsiConsole.MarkupLine($"Removing [cyan]{wt.Branch}[/]...");
                if (_gitService.RemoveWorktree(repoPath, wt.Path, force))
                {
                    AnsiConsole.MarkupLine($"  [green]✓[/] Removed {wt.Branch}");
                    removed++;
                }
                else
                {
                    AnsiConsole.MarkupLine($"  [red]✗[/] Failed to remove {wt.Branch}");
                }
            }

            AnsiConsole.MarkupLine($"\n[green]Removed {removed} of {toRemove.Count} worktree(s)[/]");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup worktrees");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private async Task<List<WorktreeInfo>> FilterWorktreesAsync(
        string repoPath,
        List<WorktreeInfo> worktrees,
        CleanupMode? mode)
    {
        return mode switch
        {
            null or Models.CleanupMode.All => worktrees,
            Models.CleanupMode.Remoteless => FilterRemoteless(repoPath, worktrees),
            Models.CleanupMode.Merged => FilterMerged(repoPath, worktrees),
            Models.CleanupMode.GitHub => await FilterGitHubAsync(repoPath, worktrees),
            Models.CleanupMode.Interactive => FilterInteractive(worktrees),
            _ => []
        };
    }

    private List<WorktreeInfo> FilterRemoteless(string repoPath, List<WorktreeInfo> worktrees)
    {
        var result = new List<WorktreeInfo>();

        foreach (var wt in worktrees)
        {
            var status = _gitService.GetBranchStatus(repoPath, wt);
            if (!status.HasRemote)
            {
                result.Add(wt);
            }
        }

        return result;
    }

    private List<WorktreeInfo> FilterMerged(string repoPath, List<WorktreeInfo> worktrees)
    {
        var result = new List<WorktreeInfo>();

        foreach (var wt in worktrees)
        {
            var status = _gitService.GetBranchStatus(repoPath, wt);
            if (status.IsMerged || status.IsIdentical)
            {
                result.Add(wt);
            }
        }

        return result;
    }

    private async Task<List<WorktreeInfo>> FilterGitHubAsync(string repoPath, List<WorktreeInfo> worktrees)
    {
        // TODO: Implement GitHub PR-based filtering
        // Need to extract owner/repo from git remote
        AnsiConsole.MarkupLine("[yellow]GitHub mode not yet fully implemented, falling back to merged[/]");
        return FilterMerged(repoPath, worktrees);
    }

    private static List<WorktreeInfo> FilterInteractive(List<WorktreeInfo> worktrees)
    {
        // TODO: Implement interactive TUI selection
        // For now, let user confirm all
        AnsiConsole.MarkupLine("[yellow]Interactive mode not yet implemented, showing all candidates[/]");
        return worktrees;
    }
}
