using System.CommandLine;
using Hypr.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Hypr.Commands;

/// <summary>
/// Lists all worktrees in the current repository.
/// </summary>
public sealed class ListCommand : Command
{
  private readonly ILogger<ListCommand> _logger;
  private readonly GitService _gitService;

  public ListCommand(
    ILogger<ListCommand> logger,
    GitService gitService)
    : base("list", "List all worktrees in the current repository")
  {
    _logger = logger;
    _gitService = gitService;

    Aliases.Add("ls");

    SetAction(Execute);
  }

  private int Execute(ParseResult _)
  {
    try
    {
      var repoPath = _gitService.FindRepoRoot();
      if (repoPath == null)
      {
        AnsiConsole.MarkupLine("[red]Error:[/] Not in a git repository");
        return 1;
      }

      var worktrees = _gitService.ListWorktrees(repoPath);

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

        if (worktree.IsCurrent) statusParts.Add("[green]current[/]");
        if (worktree.IsPrimary) statusParts.Add("[blue]primary[/]");

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
      _logger.LogError(ex, "Failed to list worktrees");
      AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
      return 1;
    }
  }
}