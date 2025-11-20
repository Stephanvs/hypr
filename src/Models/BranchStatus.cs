namespace Hyprwt.Models;

/// <summary>
/// Status information for cleanup decisions.
/// </summary>
/// <param name="Branch">The branch name.</param>
/// <param name="HasRemote">Whether the branch has a remote tracking branch.</param>
/// <param name="IsMerged">Whether the branch is merged into the default branch.</param>
/// <param name="IsIdentical">True if branch has no unique commits vs main.</param>
/// <param name="Path">The worktree path.</param>
/// <param name="HasUncommittedChanges">Whether there are uncommitted changes.</param>
public record BranchStatus(
    string Branch,
    bool HasRemote,
    bool IsMerged,
    bool IsIdentical,
    string Path,
    bool HasUncommittedChanges = false
);
