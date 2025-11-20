using Hyprwt.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Hyprwt.Commands;

/// <summary>
/// Lists all worktrees in the current repository.
/// </summary>
public class ListCommand(ILogger<ListCommand> logger, GitService gitService)
{
    /// <summary>
    /// Executes the list command.
    /// </summary>
    public int Execute(bool debug = false)
    {
        try
        {
            var repoPath = gitService.FindRepoRoot();
            if (repoPath == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Not in a git repository");
                return 1;
            }

            var worktrees = gitService.ListWorktrees(repoPath);

            if (worktrees.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No worktrees found[/]");
                return 0;
            }

            // Create a table
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("[bold]Branch[/]");
            table.AddColumn("[bold]Path[/]");
            table.AddColumn("[bold]Status[/]");

            foreach (var worktree in worktrees)
            {
                var branchDisplay = worktree.Branch;
                var pathDisplay = worktree.Path;
                var statusParts = new List<string>();

                if (worktree.IsCurrent)
                    statusParts.Add("[green]current[/]");
                if (worktree.IsPrimary)
                    statusParts.Add("[blue]primary[/]");

                var status = statusParts.Count > 0 ? string.Join(", ", statusParts) : "";

                // Highlight current worktree
                if (worktree.IsCurrent)
                {
                    branchDisplay = $"[green bold]{branchDisplay}[/]";
                    pathDisplay = $"[green]{pathDisplay}[/]";
                }

                table.AddRow(branchDisplay, pathDisplay, status);
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]Total: {worktrees.Count} worktree(s)[/]");

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list worktrees");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
