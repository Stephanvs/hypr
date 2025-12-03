namespace Hypr.Configuration;

/// <summary>
/// Configuration settings for user confirmations.
/// </summary>
public class ConfirmationsConfig
{
    /// <summary>
    /// Confirm multiple cleanups.
    /// </summary>
    public bool CleanupMultiple { get; set; } = true;

    /// <summary>
    /// Confirm force operations.
    /// </summary>
    public bool ForceOperations { get; set; } = true;
}
