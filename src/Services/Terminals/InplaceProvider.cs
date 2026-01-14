using Hypr.Configuration;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Provider that changes directory in the current process.
/// Note: This only affects the current process, not the parent shell.
/// </summary>
public class InplaceProvider : ITerminalProvider
{
    private readonly ILogger<InplaceProvider> _logger;

    public InplaceProvider(ILogger<InplaceProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "Inplace";
    public Platform SupportedPlatforms => Platform.All;
    public int Priority => 0; // Lowest priority

    public bool IsAvailable => true; // Always available

    public bool SupportsMode(TerminalMode mode) =>
        mode == TerminalMode.Inplace;

    public bool Open(string workingDirectory, TerminalMode mode, string? initCommand = null)
    {
        try
        {
            Directory.SetCurrentDirectory(workingDirectory);
            Console.WriteLine($"Changed directory to {workingDirectory}");
            _logger.LogInformation("Changed directory to {Path}", workingDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change directory to {Path}", workingDirectory);
            return false;
        }
    }
}
