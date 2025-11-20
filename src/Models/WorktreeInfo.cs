namespace Hyprwt.Models;

/// <summary>
/// Information about a single worktree.
/// </summary>
/// <param name="Branch">The branch name.</param>
/// <param name="Path">The worktree path.</param>
/// <param name="IsCurrent">Whether this is the current worktree.</param>
/// <param name="IsPrimary">Whether this is the primary (main) worktree.</param>
public record WorktreeInfo(
    string Branch,
    string Path,
    bool IsCurrent = false,
    bool IsPrimary = false
);
