using System.Diagnostics;
using Hypr.Configuration;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Terminal provider for Cursor editor.
/// </summary>
public class CursorProvider : ITerminalProvider
{
    private readonly ILogger<CursorProvider> _logger;

    public CursorProvider(ILogger<CursorProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "Cursor";
    public Platform SupportedPlatforms => Platform.All;
    public int Priority => 100;

    public bool IsAvailable
    {
        get
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Utils.PlatformUtils.IsWindows ? "where" : "which",
                    Arguments = "cursor",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                var process = Process.Start(psi);
                if (process == null) return false;

                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool SupportsMode(TerminalMode mode) =>
        mode == TerminalMode.Cursor;

    public bool Open(string workingDirectory, TerminalMode mode, string? initCommand = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cursor",
                ArgumentList = { workingDirectory },
                UseShellExecute = false
            };

            Process.Start(psi);
            _logger.LogInformation("Opened {Path} in Cursor", workingDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Cursor");
            return false;
        }
    }
}
