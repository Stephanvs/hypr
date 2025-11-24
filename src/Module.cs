using Hyprwt.Configuration;
using Hyprwt.Hooks;
using Hyprwt.Services;
using Microsoft.Extensions.DependencyInjection;

namespace hyprwt;

internal static class Module
{
  internal static IServiceCollection AddServices(this IServiceCollection services) => services
    .AddSingleton<StateService>()
    .AddSingleton<GitService>()
    .AddSingleton<GitHubService>()
    .AddSingleton<TerminalService>()
    .AddSingleton<VersionCheckService>()
    .AddSingleton<ConfigLoader>()
    .AddSingleton<HookRunner>();
}