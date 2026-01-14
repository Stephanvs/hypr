using Hypr.Configuration;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Fallback provider that just echoes the cd command.
/// </summary>
public class EchoProvider : ITerminalProvider
{
    private readonly ILogger<EchoProvider> _logger;

    public EchoProvider(ILogger<EchoProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "Echo";
    public Platform SupportedPlatforms => Platform.All;
    public int Priority => 0; // Lowest priority - fallback only

    public bool IsAvailable => true; // Always available

    public bool SupportsMode(TerminalMode mode) =>
        mode == TerminalMode.Echo;

    public bool Open(string workingDirectory, TerminalMode mode, string? initCommand = null)
    {
        Console.WriteLine($"cd {workingDirectory}");
        _logger.LogInformation("Echoed cd command for {Path}", workingDirectory);
        return true;
    }
}
