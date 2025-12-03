using System.CommandLine;
using Hyprwt.Configuration;
using Hyprwt.Models;
using Hyprwt.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

namespace Hyprwt.Commands;

/// <summary>
/// Cleans up worktrees based on various criteria.
/// </summary>
public class CleanupCommand : Command
{
    private readonly ILogger<CleanupCommand> _logger;
    private readonly GitService _gitService;
    private readonly GitHubService _gitHubService;
    private readonly IOptionsMonitor<CleanupConfig> _cleanupConfig;
    private readonly IOptionsMonitor<ConfirmationsConfig> _confirmationsConfig;

    /// <summary>
    /// Cleans up worktrees based on various criteria.
    /// </summary>
    public CleanupCommand(
        GitService gitService,
        GitHubService gitHubService,
        IOptionsMonitor<CleanupConfig> cleanupConfig,
        IOptionsMonitor<ConfirmationsConfig> confirmationsConfig,
        ILogger<CleanupCommand> logger)
        : base("cleanup", "Clean up worktrees")
    {
        _gitService = gitService;
        _gitHubService = gitHubService;
        _cleanupConfig = cleanupConfig;
        _confirmationsConfig = confirmationsConfig;
        _logger = logger;

        Aliases.Add("cl");
        Aliases.Add("clean");
        Aliases.Add("prune");
        Aliases.Add("rm");
        Aliases.Add("remove");
        Aliases.Add("del");
        Aliases.Add("delete");

        CleanupMode = new Option<CleanupMode>("--mode")
        {
            Aliases = { "-m" },
            Description = "Determine which worktrees to remove",
            DefaultValueFactory = (_ => _cleanupConfig.CurrentValue.DefaultMode)
        };

        Add(CleanupMode);
        Add(DryRun);
        Add(Force);
        Add(AutoConfirmOption);

        SetAction(ExecuteAsync);
    }

    private Option<CleanupMode> CleanupMode { get; }

    private Option<bool> DryRun { get; } = new("--dry-run")
    {
        Aliases = { "-d" },
        Description = "Show what would be removed, but don't actually remove anything"
    };

    private Option<bool> Force { get; } = new("--force")
    {
        Aliases = { "-f" },
        Description = "Force removal of worktrees and branches even with uncommitted/unpushed changes"
    };

    private AutoConfirmOption AutoConfirmOption { get; } = new();

    private async Task<int> ExecuteAsync(ParseResult ctx)
    {
        var cleanMode = ctx.GetValue(CleanupMode);
        var dryRun = ctx.GetValue(DryRun);
        var force = ctx.GetValue(Force);
        var autoConfirm = ctx.GetValue(AutoConfirmOption);

        try
        {
            var repoPath = _gitService.FindRepoRoot();
            if (repoPath == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Not in a git repository");
                return 1;
            }

            var worktrees = _gitService.ListWorktrees(repoPath);

            // Exclude primary worktree
            var candidates = worktrees.Where(w => !w.IsPrimary).ToList();

            if (candidates.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No worktrees to clean up[/]");
                return 0;
            }

            var toRemove = await FilterWorktreesAsync(repoPath, candidates, cleanMode, force);

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
                var confirmMultiple = _confirmationsConfig.CurrentValue.CleanupMultiple;
                if (confirmMultiple || toRemove.Count > 1) // Always confirm if multiple and config says so (default true)
                {
                    // Actually user logic: check confirm config.
                    // If Confirmations.CleanupMultiple is true, we confirm when multiple?
                    // "Confirm multiple cleanups" description says so.
                    // But we are asking for confirmation anyway unless autoConfirm is passed.
                }

                // Current logic was:
                if (!AnsiConsole.Confirm($"Remove {toRemove.Count} worktree(s)?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                    return 0;
                }
            }

            var removed = 0;
            var branchesDeleted = 0;
            foreach (var wt in toRemove)
            {
                AnsiConsole.MarkupLine($"Removing [cyan]{wt.Branch}[/]...");
                if (_gitService.RemoveWorktree(repoPath, wt.Path, force))
                {
                    AnsiConsole.MarkupLine($"  [green]✓[/] Removed {wt.Branch}");
                    removed++;

                    // Delete local branch after worktree removal
                    if (_gitService.BranchExistsLocally(repoPath, wt.Branch))
                    {
                        if (_gitService.DeleteBranch(repoPath, wt.Branch, force))
                        {
                            AnsiConsole.MarkupLine($"  [green]✓[/] Deleted local branch {wt.Branch}");
                            branchesDeleted++;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"  [yellow]![/] Could not delete branch {wt.Branch} (may have unmerged changes)");
                        }
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"  [red]✗[/] Failed to remove {wt.Branch}");
                }
            }

            var summary = $"Removed {removed} of {toRemove.Count} worktree(s)";
            if (branchesDeleted > 0)
            {
                summary += $", deleted {branchesDeleted} local branch(es)";
            }
            AnsiConsole.MarkupLine($"\n[green]{summary}[/]");
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
        CleanupMode? mode,
        bool force)
    {
        return mode switch
        {
            null or Configuration.CleanupMode.All => FilterUncommittedChanges(worktrees, force),
            Configuration.CleanupMode.Remoteless => FilterUncommittedChanges(FilterRemoteless(repoPath, worktrees), force),
            Configuration.CleanupMode.Merged => FilterUncommittedChanges(FilterMerged(repoPath, worktrees), force),
            Configuration.CleanupMode.GitHub => FilterUncommittedChanges(await FilterGitHubAsync(repoPath, worktrees), force),
            Configuration.CleanupMode.Interactive => FilterUncommittedChanges(await FilterInteractiveAsync(worktrees), force),
            _ => []
        };
    }

    private List<WorktreeInfo> FilterUncommittedChanges(List<WorktreeInfo> worktrees, bool force)
    {
        if (force)
            return worktrees;

        var result = new List<WorktreeInfo>();
        foreach (var wt in worktrees)
        {
            if (!_gitService.HasUncommittedChanges(wt.Path))
            {
                result.Add(wt);
            }
            else
            {
                AnsiConsole.MarkupLine($"  [dim]Skipping {wt.Branch} - has uncommitted changes[/]");
            }
        }
        return result;
    }

    private List<WorktreeInfo> FilterRemoteless(string repoPath, List<WorktreeInfo> worktrees)
    {
        var result = new List<WorktreeInfo>();

        foreach (var wt in worktrees)
        {
            var status = _gitService.GetBranchStatus(repoPath, wt);

            // Safe to remove if no remote AND no uncommitted/unpushed changes
            var isSafeToRemove = !status.HasRemote &&
                                 (!status.HasUncommittedChanges && !status.HasUnpushedCommits);

            if (isSafeToRemove)
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

            // Safe to remove if merged/identical AND no uncommitted/unpushed changes
            var isSafeToRemove = (status.IsMerged || status.IsIdentical) &&
                                 (!status.HasUncommittedChanges && !status.HasUnpushedCommits);

            if (isSafeToRemove)
            {
                result.Add(wt);
            }
        }

        return result;
    }

    private async Task<List<WorktreeInfo>> FilterGitHubAsync(string repoPath, List<WorktreeInfo> worktrees)
    {
        // Check if gh CLI is available and authenticated
        if (!await _gitHubService.IsGitHubCliAvailable())
        {
            AnsiConsole.MarkupLine("[red]Error:[/] GitHub CLI (gh) is not installed or not authenticated. Please install gh and run 'gh auth login' first.");
            return [];
        }

        // Check if this is a GitHub repository
        var (isGitHubRepo, _) = _gitService.IsGitHubRepo(repoPath);
        if (!isGitHubRepo)
        {
            AnsiConsole.MarkupLine("[yellow]Not a GitHub repository, no worktrees will be removed[/]");
            return [];
        }

        var result = new List<WorktreeInfo>();

        foreach (var wt in worktrees)
        {
            try
            {
                var prStatus = await _gitHubService.GetPullRequestStatusForBranch(repoPath, wt.Branch);

                switch (prStatus)
                {
                    // Only remove if PR is merged or closed
                    case PrStatus.Merged or PrStatus.Closed:
                        result.Add(wt);
                        AnsiConsole.MarkupLine($"  [dim]Branch {wt.Branch} has {prStatus.ToString().ToLower()} PR[/]");
                        break;
                    case PrStatus.Open:
                        AnsiConsole.MarkupLine($"  [dim]Skipping {wt.Branch} - PR is still open[/]");
                        break;
                    default:
                        AnsiConsole.MarkupLine($"  [dim]Skipping {wt.Branch} - no PR found[/]");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check PR status for branch {Branch}", wt.Branch);
                AnsiConsole.MarkupLine($"  [dim]Skipping {wt.Branch} - failed to check PR status[/]");
            }
        }

        return result;
    }

    private async Task<List<WorktreeInfo>> FilterInteractiveAsync(List<WorktreeInfo> worktrees)
    {
        _logger.LogDebug("FilterInteractiveAsync entered with {Count} worktrees", worktrees.Count);
        var repoPath = _gitService.FindRepoRoot();
        if (repoPath == null)
        {
            _logger.LogDebug("FilterInteractiveAsync exiting early: not in a git repository");
            AnsiConsole.MarkupLine("[red]Error:[/] Not in a git repository");
            return [];
        }

        // Create worktree items with status for the selection prompt
        var worktreeItems = worktrees.Select(wt =>
        {
            var status = _gitService.GetBranchStatus(repoPath, wt);
            return new WorktreeSelectionItem(wt, status);
        }).ToList();

        var prompt = new MultiSelectionPrompt<WorktreeSelectionItem>()
            .Title("Select worktrees to cleanup:")
            .NotRequired()
            .PageSize(15)
            .MoreChoicesText("[grey](Move up and down to reveal more worktrees)[/]")
            .InstructionsText(
                "[grey](Press [blue]<space>[/] to toggle a worktree, " +
                "[green]<enter>[/] to accept selected worktrees[/]");

        // Add choices to the prompt
        prompt.AddChoices(worktreeItems);
        
        // Use a converter to format the display
        prompt.Converter = item =>
        {
            var statusText = GetStatusDisplay(item.Status);
            return $"[cyan]{item.Worktree.Branch}[/] [dim]{item.Worktree.Path}[/] {statusText}";
        };

        var selected = AnsiConsole.Prompt(prompt);
        var result = selected.Select(item => item.Worktree).ToList();
        _logger.LogDebug("FilterInteractiveAsync exiting with {Count} selected worktrees", result.Count);

        return result;
    }

    private static string GetStatusDisplay(BranchStatus status)
    {
        var indicators = new List<string>();

        if (status.HasUncommittedChanges)
            indicators.Add("[red]![/]");
        if (status.HasUnpushedCommits)
            indicators.Add("[yellow]↑[/]");
        if (status is { HasUncommittedChanges: false, HasUnpushedCommits: false })
            indicators.Add("[green]✓[/]");

        return indicators.Count > 0 ? $"[dim]{string.Join(" ", indicators)}[/]" : "";
    }

    /// <summary>
    /// Helper class to hold worktree info with status for the selection prompt.
    /// </summary>
    private record WorktreeSelectionItem(
        WorktreeInfo Worktree,
        BranchStatus Status
    );
}
