using FakeItEasy;
using Hypr.Configuration;
using Hypr.Services.Terminals;
using Microsoft.Extensions.Logging;

namespace test.Services.Terminals;

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class EchoProviderTests
{
    private readonly EchoProvider _provider;

    public EchoProviderTests()
    {
        var logger = A.Fake<ILogger<EchoProvider>>();
        _provider = new EchoProvider(logger);
    }

    [Fact]
    public void Name_ReturnsEcho()
    {
        Assert.Equal("Echo", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsAll()
    {
        Assert.Equal(Platform.All, _provider.SupportedPlatforms);
    }

    [Fact]
    public void Priority_ReturnsZero()
    {
        Assert.Equal(0, _provider.Priority);
    }

    [Fact]
    public void IsAvailable_ReturnsTrue()
    {
        Assert.True(_provider.IsAvailable);
    }

    [Theory]
    [InlineData(TerminalMode.Echo, true)]
    [InlineData(TerminalMode.Tab, false)]
    [InlineData(TerminalMode.Window, false)]
    [InlineData(TerminalMode.VSCode, false)]
    [InlineData(TerminalMode.Cursor, false)]
    [InlineData(TerminalMode.Inplace, false)]
    public void SupportsMode_ReturnsExpectedValue(TerminalMode mode, bool expected)
    {
        Assert.Equal(expected, _provider.SupportsMode(mode));
    }

    [Fact]
    public void Open_ReturnsTrue()
    {
        var result = _provider.Open("/test/path", TerminalMode.Echo);

        Assert.True(result);
    }
}

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class InplaceProviderTests
{
    private readonly InplaceProvider _provider;

    public InplaceProviderTests()
    {
        var logger = A.Fake<ILogger<InplaceProvider>>();
        _provider = new InplaceProvider(logger);
    }

    [Fact]
    public void Name_ReturnsInplace()
    {
        Assert.Equal("Inplace", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsAll()
    {
        Assert.Equal(Platform.All, _provider.SupportedPlatforms);
    }

    [Fact]
    public void Priority_ReturnsZero()
    {
        Assert.Equal(0, _provider.Priority);
    }

    [Fact]
    public void IsAvailable_ReturnsTrue()
    {
        Assert.True(_provider.IsAvailable);
    }

    [Theory]
    [InlineData(TerminalMode.Inplace, true)]
    [InlineData(TerminalMode.Echo, false)]
    [InlineData(TerminalMode.Tab, false)]
    [InlineData(TerminalMode.Window, false)]
    [InlineData(TerminalMode.VSCode, false)]
    [InlineData(TerminalMode.Cursor, false)]
    public void SupportsMode_ReturnsExpectedValue(TerminalMode mode, bool expected)
    {
        Assert.Equal(expected, _provider.SupportsMode(mode));
    }

    [Fact]
    public void Open_WithValidDirectory_ChangesDirectoryAndReturnsTrue()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var tempDir = Path.GetTempPath();

        try
        {
            var result = _provider.Open(tempDir, TerminalMode.Inplace);

            Assert.True(result);
            Assert.Equal(tempDir.TrimEnd(Path.DirectorySeparatorChar), Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public void Open_WithInvalidDirectory_ReturnsFalse()
    {
        var result = _provider.Open("/nonexistent/path/that/does/not/exist", TerminalMode.Inplace);

        Assert.False(result);
    }
}

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class VSCodeProviderTests
{
    private readonly VSCodeProvider _provider;

    public VSCodeProviderTests()
    {
        var logger = A.Fake<ILogger<VSCodeProvider>>();
        _provider = new VSCodeProvider(logger);
    }

    [Fact]
    public void Name_ReturnsVSCode()
    {
        Assert.Equal("VS Code", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsAll()
    {
        Assert.Equal(Platform.All, _provider.SupportedPlatforms);
    }

    [Fact]
    public void Priority_Returns100()
    {
        Assert.Equal(100, _provider.Priority);
    }

    [Theory]
    [InlineData(TerminalMode.VSCode, true)]
    [InlineData(TerminalMode.Echo, false)]
    [InlineData(TerminalMode.Tab, false)]
    [InlineData(TerminalMode.Window, false)]
    [InlineData(TerminalMode.Cursor, false)]
    [InlineData(TerminalMode.Inplace, false)]
    public void SupportsMode_ReturnsExpectedValue(TerminalMode mode, bool expected)
    {
        Assert.Equal(expected, _provider.SupportsMode(mode));
    }
}

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class CursorProviderTests
{
    private readonly CursorProvider _provider;

    public CursorProviderTests()
    {
        var logger = A.Fake<ILogger<CursorProvider>>();
        _provider = new CursorProvider(logger);
    }

    [Fact]
    public void Name_ReturnsCursor()
    {
        Assert.Equal("Cursor", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsAll()
    {
        Assert.Equal(Platform.All, _provider.SupportedPlatforms);
    }

    [Fact]
    public void Priority_Returns100()
    {
        Assert.Equal(100, _provider.Priority);
    }

    [Theory]
    [InlineData(TerminalMode.Cursor, true)]
    [InlineData(TerminalMode.Echo, false)]
    [InlineData(TerminalMode.Tab, false)]
    [InlineData(TerminalMode.Window, false)]
    [InlineData(TerminalMode.VSCode, false)]
    [InlineData(TerminalMode.Inplace, false)]
    public void SupportsMode_ReturnsExpectedValue(TerminalMode mode, bool expected)
    {
        Assert.Equal(expected, _provider.SupportsMode(mode));
    }
}

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class WindowsTerminalProviderTests
{
    private readonly WindowsTerminalProvider _provider;

    public WindowsTerminalProviderTests()
    {
        var logger = A.Fake<ILogger<WindowsTerminalProvider>>();
        _provider = new WindowsTerminalProvider(logger);
    }

    [Fact]
    public void Name_ReturnsWindowsTerminal()
    {
        Assert.Equal("Windows Terminal", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsWindows()
    {
        Assert.Equal(Platform.Windows, _provider.SupportedPlatforms);
    }

    [Fact]
    public void Priority_Returns100()
    {
        Assert.Equal(100, _provider.Priority);
    }

    [Theory]
    [InlineData(TerminalMode.Tab, true)]
    [InlineData(TerminalMode.Window, true)]
    [InlineData(TerminalMode.Echo, false)]
    [InlineData(TerminalMode.VSCode, false)]
    [InlineData(TerminalMode.Cursor, false)]
    [InlineData(TerminalMode.Inplace, false)]
    public void SupportsMode_ReturnsExpectedValue(TerminalMode mode, bool expected)
    {
        Assert.Equal(expected, _provider.SupportsMode(mode));
    }
}

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class TmuxProviderTests
{
    private readonly TmuxProvider _provider;

    public TmuxProviderTests()
    {
        var logger = A.Fake<ILogger<TmuxProvider>>();
        _provider = new TmuxProvider(logger);
    }

    [Fact]
    public void Name_ReturnsTmux()
    {
        Assert.Equal("tmux", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsLinuxAndMacOS()
    {
        Assert.Equal(Platform.Linux | Platform.MacOS, _provider.SupportedPlatforms);
    }

    [Fact]
    public void Priority_Returns150()
    {
        Assert.Equal(150, _provider.Priority);
    }

    [Theory]
    [InlineData(TerminalMode.Tab, true)]
    [InlineData(TerminalMode.Window, true)]
    [InlineData(TerminalMode.Echo, false)]
    [InlineData(TerminalMode.VSCode, false)]
    [InlineData(TerminalMode.Cursor, false)]
    [InlineData(TerminalMode.Inplace, false)]
    public void SupportsMode_ReturnsExpectedValue(TerminalMode mode, bool expected)
    {
        Assert.Equal(expected, _provider.SupportsMode(mode));
    }
}

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class GnomeTerminalProviderTests
{
    private readonly GnomeTerminalProvider _provider;

    public GnomeTerminalProviderTests()
    {
        var logger = A.Fake<ILogger<GnomeTerminalProvider>>();
        _provider = new GnomeTerminalProvider(logger);
    }

    [Fact]
    public void Name_ReturnsGnomeTerminal()
    {
        Assert.Equal("GNOME Terminal", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsLinux()
    {
        Assert.Equal(Platform.Linux, _provider.SupportedPlatforms);
    }

    [Fact]
    public void Priority_Returns50()
    {
        Assert.Equal(50, _provider.Priority);
    }

    [Theory]
    [InlineData(TerminalMode.Tab, true)]
    [InlineData(TerminalMode.Window, true)]
    [InlineData(TerminalMode.Echo, false)]
    [InlineData(TerminalMode.VSCode, false)]
    [InlineData(TerminalMode.Cursor, false)]
    [InlineData(TerminalMode.Inplace, false)]
    public void SupportsMode_ReturnsExpectedValue(TerminalMode mode, bool expected)
    {
        Assert.Equal(expected, _provider.SupportsMode(mode));
    }
}

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class ITerm2ProviderTests
{
    private readonly ITerm2Provider _provider;

    public ITerm2ProviderTests()
    {
        var logger = A.Fake<ILogger<ITerm2Provider>>();
        _provider = new ITerm2Provider(logger);
    }

    [Fact]
    public void Name_ReturnsITerm2()
    {
        Assert.Equal("iTerm2", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsMacOS()
    {
        Assert.Equal(Platform.MacOS, _provider.SupportedPlatforms);
    }

    [Fact]
    public void Priority_Returns100()
    {
        Assert.Equal(100, _provider.Priority);
    }

    [Theory]
    [InlineData(TerminalMode.Tab, true)]
    [InlineData(TerminalMode.Window, true)]
    [InlineData(TerminalMode.Echo, false)]
    [InlineData(TerminalMode.VSCode, false)]
    [InlineData(TerminalMode.Cursor, false)]
    [InlineData(TerminalMode.Inplace, false)]
    public void SupportsMode_ReturnsExpectedValue(TerminalMode mode, bool expected)
    {
        Assert.Equal(expected, _provider.SupportsMode(mode));
    }
}

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class TerminalAppProviderTests
{
    private readonly TerminalAppProvider _provider;

    public TerminalAppProviderTests()
    {
        var logger = A.Fake<ILogger<TerminalAppProvider>>();
        _provider = new TerminalAppProvider(logger);
    }

    [Fact]
    public void Name_ReturnsTerminalApp()
    {
        Assert.Equal("Terminal.app", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsMacOS()
    {
        Assert.Equal(Platform.MacOS, _provider.SupportedPlatforms);
    }

    [Fact]
    public void Priority_Returns50()
    {
        Assert.Equal(50, _provider.Priority);
    }

    [Fact]
    public void IsAvailable_ReturnsTrue()
    {
        // Terminal.app is always available on macOS
        Assert.True(_provider.IsAvailable);
    }

    [Theory]
    [InlineData(TerminalMode.Tab, true)]
    [InlineData(TerminalMode.Window, true)]
    [InlineData(TerminalMode.Echo, false)]
    [InlineData(TerminalMode.VSCode, false)]
    [InlineData(TerminalMode.Cursor, false)]
    [InlineData(TerminalMode.Inplace, false)]
    public void SupportsMode_ReturnsExpectedValue(TerminalMode mode, bool expected)
    {
        Assert.Equal(expected, _provider.SupportsMode(mode));
    }
}
