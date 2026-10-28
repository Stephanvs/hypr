using System.Globalization;
using System.Text.Json;
using Hypr.Configuration;
using Hypr.Hooks;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace Hypr.Services;

public record PullRequestWorktree(string RepositoryPath, string Remote, int Number, string Url, string Branch, string Path);

public class PullRequestWorktreeService(
    GitService git, HookRunner hooks, TerminalService terminals,
    IOptionsMonitor<WorktreeConfig> worktreeConfig, IOptionsMonitor<ScriptsConfig> scripts)
{
    public async Task<PullRequestWorktree> ResolveAsync(string input, DirectoryInfo? directory)
    {
        if (!(int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0)
            && !TryParsePullRequestUrl(input, out _, out _))
            throw new ArgumentException("Specify a positive PR number or an HTTPS pull request URL.");

        var root = git.FindRepoRoot() ?? throw new InvalidOperationException("Not in a git repository.");
        using var repository = new Repository(root);
        root = repository.Info.WorkingDirectory ?? root;
        var (exitCode, stdout, stderr) = await GitHubService.RunProcessAsync(
            "gh", ["pr", "view", input, "--json", "url"], root, TimeSpan.FromSeconds(30));
        if (exitCode != 0)
            throw new InvalidOperationException($"Could not resolve PR. Check 'gh auth status'. {stderr.Trim()}");
        using var document = JsonDocument.Parse(stdout);
        var url = document.RootElement.GetProperty("url").GetString() ?? "";
        if (!TryParsePullRequestUrl(url, out var identity, out number))
            throw new InvalidOperationException("GitHub returned an invalid pull request URL.");
        var remote = repository.Network.Remotes.FirstOrDefault(r => RemoteIdentity(r.Url) == identity);
        if (remote == null)
            throw new InvalidOperationException("The PR repository does not match any remote in this repository.");

        var branch = $"pr-{number}";
        var existing = git.ListWorktrees(root).FirstOrDefault(w => w.Branch == branch);
        var path = existing?.Path ?? worktreeConfig.CurrentValue.GetPath(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), branch, directory);
        return new(root, remote.Name, number, url, branch, path);
    }

    public bool Exists(PullRequestWorktree worktree) =>
        git.ListWorktrees(worktree.RepositoryPath).Any(w => w.Branch == worktree.Branch);

    public async Task CreateAsync(PullRequestWorktree worktree)
    {
        using var repository = new Repository(worktree.RepositoryPath);
        var key = $"branch.{worktree.Branch}.hypr-pr-url";
        if (repository.Branches[worktree.Branch] != null)
        {
            if (repository.Config.Get<string>(key)?.Value != worktree.Url)
                throw new InvalidOperationException($"Branch '{worktree.Branch}' already exists and is not associated with this PR.");
            if (Exists(worktree)) return;
        }
        else
        {
            var fetchRef = $"refs/hypr/pr-fetch/{Guid.NewGuid():N}";
            try
            {
                var (exitCode, _, stderr) = await GitHubService.RunProcessAsync("git",
                    ["fetch", "--no-tags", "--no-write-fetch-head", worktree.Remote, $"refs/pull/{worktree.Number}/head:{fetchRef}"],
                    worktree.RepositoryPath, TimeSpan.FromSeconds(60));
                if (exitCode != 0) throw new InvalidOperationException($"Could not fetch PR: {stderr.Trim()}");
                var commit = repository.Lookup<Commit>(fetchRef)
                    ?? throw new InvalidOperationException("Fetched PR has no commit.");
                repository.CreateBranch(worktree.Branch, commit);
                repository.Config.Set(key, worktree.Url);
            }
            finally
            {
                if (repository.Refs[fetchRef] != null) repository.Refs.Remove(fetchRef);
            }
        }

        if (!hooks.RunHook(scripts.CurrentValue.PreCreate, worktree.RepositoryPath))
            throw new InvalidOperationException("Pre-create hook failed.");
        if (!git.CreateWorktree(worktree.RepositoryPath, worktree.Path, worktree.Branch))
            throw new InvalidOperationException("Could not create PR worktree.");
        if (!hooks.RunHook(scripts.CurrentValue.PostCreate, worktree.Path))
            throw new InvalidOperationException($"Post-create hook failed. Worktree is available at {worktree.Path}.");
        hooks.RunHook(scripts.CurrentValue.PostCreateAsync, worktree.Path, async: true);
    }

    public bool Open(PullRequestWorktree worktree, TerminalMode mode, string? afterInit) =>
        terminals.OpenWorktree(worktree.Path, mode, scripts.CurrentValue.SessionInit, afterInit);

    private static bool TryParsePullRequestUrl(string input, out string identity, out int number)
    {
        identity = "";
        number = 0;
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri) || uri.Scheme != "https") return false;
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        if (parts.Length != 4 || parts[2] != "pull" ||
            !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out number) || number <= 0) return false;
        identity = $"{uri.Host}/{parts[0]}/{parts[1]}".ToLowerInvariant();
        return true;
    }

    private static string RemoteIdentity(string remote)
    {
        string identity;
        if (Uri.TryCreate(remote, UriKind.Absolute, out var uri) && !uri.IsFile)
            identity = uri.Host + "/" + uri.AbsolutePath.Trim('/');
        else
        {
            var at = remote.IndexOf('@');
            identity = remote[(at + 1)..].Replace(':', '/').TrimEnd('/');
        }
        return (identity.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? identity[..^4] : identity).ToLowerInvariant();
    }
}
