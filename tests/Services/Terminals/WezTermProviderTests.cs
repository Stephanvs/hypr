using FakeItEasy;
using Hypr.Configuration;
using Hypr.Services.Terminals;
using Microsoft.Extensions.Logging;

namespace test.Services.Terminals;

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class WezTermProviderTests
{
    private readonly WezTermProvider _provider;

    public WezTermProviderTests()
    {
        var logger = A.Fake<ILogger<WezTermProvider>>();
        _provider = new WezTermProvider(logger);
    }

    [Fact]
    public void Name_ReturnsWezTerm()
    {
        Assert.Equal("WezTerm", _provider.Name);
    }

    [Fact]
    public void SupportedPlatforms_ReturnsAll()
    {
        Assert.Equal(Platform.All, _provider.SupportedPlatforms);
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
