namespace Hyprwt.Configuration;

/// <summary>
/// Configuration settings for terminal management.
/// </summary>
public class TerminalConfig
{
    /// <summary>
    /// Terminal mode (tab/window/inplace/echo/vscode/cursor).
    /// </summary>
    public TerminalMode Mode { get; set; } = TerminalMode.Window;

    /// <summary>
    /// Always create new session.
    /// </summary>
    public bool AlwaysNew { get; set; } = false;

    /// <summary>
    /// Custom terminal program.
    /// </summary>
    public string? Program { get; set; }
}

/// <summary>
/// Terminal switching modes.
/// </summary>
public enum TerminalMode
{
    /// <summary>Open in new tab.</summary>
    Tab,

    /// <summary>Open in new window.</summary>
    Window,

    /// <summary>Change directory in current session.</summary>
    Inplace,

    /// <summary>Print cd command only.</summary>
    Echo,

    /// <summary>Open in VSCode.</summary>
    VSCode,

    /// <summary>Open in Cursor.</summary>
    Cursor
}
