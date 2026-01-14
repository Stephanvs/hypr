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
    // Commands and terminal providers (explicit registration for commands, Scrutor for terminal providers)
    .AddCommands()
    .AddTerminalProviders();

  internal static IServiceCollection AddCommands(this IServiceCollection services) => services
    .AddSingleton<Command, ListCommand>()
    .AddSingleton<Command, SwitchCommand>()
    .AddSingleton<Command, ConfigCommand>()
    .AddSingleton<Command, CleanupCommand>();

  private static IServiceCollection AddTerminalProviders(this IServiceCollection services)
  {
    var currentPlatform = GetCurrentPlatform();

    // Use Scrutor to scan and register all terminal providers that support current platform
    services.Scan(scan => scan
        .FromAssemblyOf<ITerminalProvider>()
        .AddClasses(classes => classes
            .AssignableTo<ITerminalProvider>()
            .Where(type => SupportsCurrentPlatform(type, currentPlatform)))
        .AsImplementedInterfaces()
        .WithSingletonLifetime());

    return services;
  }

  private static Platform GetCurrentPlatform()
  {
    if (PlatformUtils.IsWindows) return Platform.Windows;
    if (PlatformUtils.IsMacOS) return Platform.MacOS;
    if (PlatformUtils.IsLinux) return Platform.Linux;
    return Platform.None;
  }

  private static bool SupportsCurrentPlatform(Type providerType, Platform currentPlatform)
  {
    return GetSupportedPlatformsForType(providerType).HasFlag(currentPlatform);
  }

  private static Platform GetSupportedPlatformsForType(Type providerType)
  {
    // Map known provider types to their supported platforms
    return providerType.Name switch
    {
      nameof(WindowsTerminalProvider) => Platform.Windows,
      nameof(ITerm2Provider) => Platform.MacOS,
      nameof(TerminalAppProvider) => Platform.MacOS,
      nameof(GnomeTerminalProvider) => Platform.Linux,
      nameof(TmuxProvider) => Platform.Linux | Platform.MacOS,
      nameof(VSCodeProvider) => Platform.All,
      nameof(CursorProvider) => Platform.All,
      nameof(EchoProvider) => Platform.All,
      nameof(InplaceProvider) => Platform.All,
      _ => Platform.None
    };
  }
}