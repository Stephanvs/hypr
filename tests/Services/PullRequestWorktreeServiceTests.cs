using FakeItEasy;
using Hypr.Configuration;
using Hypr.Hooks;
using Hypr.Services;
using LibGit2Sharp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace test.Services;

public class PullRequestWorktreeServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "hypr-pr-tests-" + Guid.NewGuid());
    private readonly PullRequestWorktreeService _service;
    private readonly PullRequestWorktree _worktree;
    private readonly string _pullRequestCommit;
    private static readonly Signature Author = new("Hypr test", "test@example.com", DateTimeOffset.UtcNow);

    public PullRequestWorktreeServiceTests()
    {
        Directory.CreateDirectory(_directory);
        var remotePath = Path.Combine(_directory, "remote");
        Repository.Init(remotePath);
        using (var remote = new Repository(remotePath))
        {
            File.WriteAllText(Path.Combine(remotePath, "base.txt"), "base");
            Commands.Stage(remote, "base.txt");
            var baseline = remote.Commit("base", Author, Author);
            File.WriteAllText(Path.Combine(remotePath, "review.txt"), "PR content");
            Commands.Stage(remote, "review.txt");
            var head = remote.Commit("PR change", Author, Author);
            _pullRequestCommit = head.Sha;
            remote.Refs.Add("refs/pull/42/head", head.Id);
            remote.Reset(ResetMode.Hard, baseline);
        }
        var localPath = Path.Combine(_directory, "local");
        Repository.Clone(remotePath, localPath);
        _worktree = new(localPath, "origin", 42, "https://github.com/example/project/pull/42",
            "pr-42", Path.Combine(_directory, "review"));
        var git = new GitService(NullLogger<GitService>.Instance);
        var terminalConfig = Monitor(new TerminalConfig());
        _service = new(git, new HookRunner(NullLogger<HookRunner>.Instance),
            new TerminalService(NullLogger<TerminalService>.Instance, [], terminalConfig),
            Monitor(new WorktreeConfig()), Monitor(new ScriptsConfig()));
    }

    [Fact]
    public async Task CreateAndReopen_PreservesReviewCommitAndLocalWork()
    {
        await _service.CreateAsync(_worktree);
        Assert.Equal("PR content", File.ReadAllText(Path.Combine(_worktree.Path, "review.txt")));
        using var review = new Repository(_worktree.Path);
        Assert.Equal(_pullRequestCommit, review.Head.Tip.Sha);
        File.WriteAllText(Path.Combine(_worktree.Path, "local.txt"), "local commit");
        Commands.Stage(review, "local.txt");
        var localCommit = review.Commit("local work", Author, Author);
        File.WriteAllText(Path.Combine(_worktree.Path, "review.txt"), "uncommitted review edits");

        await _service.CreateAsync(_worktree);

        Assert.Equal(localCommit.Sha, review.Head.Tip.Sha);
        Assert.Equal("uncommitted review edits", File.ReadAllText(Path.Combine(_worktree.Path, "review.txt")));
        Assert.True(_service.Exists(_worktree));
    }

    [Fact]
    public async Task ExistingUnrelatedBranch_IsRejectedWithoutChangingIt()
    {
        using var local = new Repository(_worktree.RepositoryPath);
        var original = local.CreateBranch(_worktree.Branch).Tip.Sha;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(_worktree));

        Assert.Contains("not associated", exception.Message);
        Assert.Equal(original, local.Branches[_worktree.Branch].Tip.Sha);
        Assert.False(Directory.Exists(_worktree.Path));
    }

    [Fact]
    public async Task MissingPullRequestRef_DoesNotCreateBranchOrWorktree()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(_worktree with { Number = 999 }));

        using var local = new Repository(_worktree.RepositoryPath);
        Assert.Null(local.Branches[_worktree.Branch]);
        Assert.False(Directory.Exists(_worktree.Path));
    }

    private static IOptionsMonitor<T> Monitor<T>(T value) where T : class
    {
        var monitor = A.Fake<IOptionsMonitor<T>>();
        A.CallTo(() => monitor.CurrentValue).Returns(value);
        return monitor;
    }

    public void Dispose()
    {
        foreach (var file in Directory.EnumerateFiles(_directory, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_directory, recursive: true);
    }
}
