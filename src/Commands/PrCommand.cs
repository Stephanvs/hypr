using System.CommandLine;
using Hypr.Configuration;
using Hypr.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace Hypr.Commands;

public sealed class PrCommand : Command
{
    private Argument<string> PullRequest { get; } = new("pull-request") { Description = "PR number or HTTPS URL" };
    private Option<DirectoryInfo?> DirectoryOption { get; } = new("--dir") { Description = "Directory for a new worktree" };
    private Option<TerminalMode?> TerminalOption { get; } = new("--terminal") { Description = "Terminal mode" };
    private Option<string?> AfterInit { get; } = new("--after-init") { Description = "Command to run after session init" };
    private AutoConfirmOption Yes { get; } = new();

    public PrCommand(PullRequestWorktreeService service, IOptionsMonitor<TerminalConfig> terminal, ILogger<PrCommand> logger)
        : base("pr", "Open a GitHub pull request in a worktree")
    {
        Add(PullRequest);
        Add(DirectoryOption);
        Add(TerminalOption);
        Add(AfterInit);
        Add(Yes);
        SetAction(async context =>
        {
            try
            {
                var worktree = await service.ResolveAsync(context.GetRequiredValue(PullRequest), context.GetValue(DirectoryOption));
                if (!service.Exists(worktree) && !context.GetValue(Yes)
                    && !AnsiConsole.Confirm($"Create PR #{worktree.Number} worktree at {Markup.Escape(worktree.Path)}?"))
                    return 0;
                await service.CreateAsync(worktree);
                AnsiConsole.MarkupLine($"[green]Opening PR #{worktree.Number}:[/] {Markup.Escape(worktree.Path)}");
                return service.Open(worktree, context.GetValue(TerminalOption) ?? terminal.CurrentValue.Mode,
                    context.GetValue(AfterInit)) ? 0 : 1;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to open pull request worktree");
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                return 1;
            }
        });
    }
}
