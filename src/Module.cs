using Hyprwt.Configuration;
using Hyprwt.Hooks;
using Hyprwt.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace hyprwt;

internal static class Module
{
  internal static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration) => services
    .Configure<TerminalConfig>(configuration.GetSection("terminal"))
    .Configure<WorktreeConfig>(configuration.GetSection("worktree"))
    .Configure<CleanupConfig>(configuration.GetSection("cleanup"))
    .Configure<ScriptsConfig>(configuration.GetSection("scripts"))
    .Configure<ConfirmationsConfig>(configuration.GetSection("confirmations"))
    .AddSingleton<StateService>()
    .AddSingleton<GitService>()
    .AddSingleton<GitHubService>()
    .AddSingleton<TerminalService>()
    .AddSingleton<VersionCheckService>()
    .AddSingleton<HookRunner>();
}
