using Hypr.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Hypr.Logging;

public static class LoggingExtensions
{
    public static IServiceCollection AddHyprLogging(this IServiceCollection services, string[] args)
    {
        // Check for debug flag in environment or args
        var isDebug = args.Contains("--debug") || 
            (Environment.GetEnvironmentVariable("HYPR_DEBUG")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(isDebug ? LogEventLevel.Debug : LogEventLevel.Information)
            .WriteTo.File(PathProvider.GetLogFilePath(), 
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Console(
                outputTemplate: isDebug 
                    ? "{Message:lj}{NewLine}{Exception}" 
                    : "[{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: isDebug ? LogEventLevel.Debug : LogEventLevel.Warning)
            .CreateLogger();

        // Use Serilog
        services.AddLogging(x => x.ClearProviders().AddSerilog());
        
        return services;
    }
}