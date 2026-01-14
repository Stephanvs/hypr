using System.CommandLine;
using Hypr.Commands;
using Hypr.Configuration;
using Hypr.Hooks;
using Hypr.Services;
using Hypr.Services.Terminals;
using Hypr.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hypr;

internal static class Module
{
  internal static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration) => services
    // Configuration sections
    .Configure<TerminalConfig>(configuration.GetSection("terminal"))
    .Configure<WorktreeConfig>(configuration.GetSection("worktree"))
    .Configure<CleanupConfig>(configuration.GetSection("cleanup"))
    .Configure<ScriptsConfig>(configuration.GetSection("scripts"))
    .Configure<ConfirmationsConfig>(configuration.GetSection("confirmations"))
    // Services
    .AddSingleton<StateService>()
    .AddSingleton<GitService>()
    .AddSingleton<GitHubService>()
    .AddSingleton<TerminalService>()
    .AddSingleton<VersionCheckService>()
    .AddSingleton<HookRunner>()
    // Commands (explicit registration for trimming/AOT compatibility)
    .AddCommands();

  internal static IServiceCollection AddCommands(this IServiceCollection services) => services
    .AddSingleton<Command, ListCommand>()
    .AddSingleton<Command, SwitchCommand>()
    .AddSingleton<Command, ConfigCommand>()
    .AddSingleton<Command, CleanupCommand>();
}
