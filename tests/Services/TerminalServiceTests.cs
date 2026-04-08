using FakeItEasy;
using Hypr.Configuration;
using Hypr.Services;
using Hypr.Services.Terminals;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace test.Services;

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class TerminalServiceTests
{
    private readonly ILogger<TerminalService> _logger;
    private readonly IOptionsMonitor<TerminalConfig> _terminalConfig;

    public TerminalServiceTests()
    {
        _logger = A.Fake<ILogger<TerminalService>>();
        _terminalConfig = A.Fake<IOptionsMonitor<TerminalConfig>>();
        A.CallTo(() => _terminalConfig.CurrentValue).Returns(new TerminalConfig());
    }

    private TerminalService CreateService(IEnumerable<ITerminalProvider> providers, TerminalConfig? terminalConfig = null)
    {
        if (terminalConfig != null)
        {
            A.CallTo(() => _terminalConfig.CurrentValue).Returns(terminalConfig);
        }

        return new TerminalService(_logger, providers, _terminalConfig);
    }

    [Fact]
    public void OpenWorktree_WithNoProviders_ReturnsFalse()
    {
        var service = CreateService([]);

        var result = service.OpenWorktree("/some/path", TerminalMode.Tab);

        Assert.False(result);
    }

    [Fact]
    public void OpenWorktree_WithMatchingProvider_CallsProviderOpen()
    {
        var provider = A.Fake<ITerminalProvider>();
        A.CallTo(() => provider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => provider.IsAvailable).Returns(true);
        A.CallTo(() => provider.Priority).Returns(100);
        A.CallTo(() => provider.Open("/test/path", TerminalMode.Tab, null)).Returns(true);

        var service = CreateService([provider]);

        var result = service.OpenWorktree("/test/path", TerminalMode.Tab);

        Assert.True(result);
        A.CallTo(() => provider.Open("/test/path", TerminalMode.Tab, null)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void OpenWorktree_WithUnavailableProvider_ReturnsFalse()
    {
        var provider = A.Fake<ITerminalProvider>();
        A.CallTo(() => provider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => provider.IsAvailable).Returns(false);

        var service = CreateService([provider]);

        var result = service.OpenWorktree("/test/path", TerminalMode.Tab);

        Assert.False(result);
        A.CallTo(() => provider.Open(A<string>._, A<TerminalMode>._, A<string?>._)).MustNotHaveHappened();
    }

    [Fact]
    public void OpenWorktree_WithUnsupportedMode_ReturnsFalse()
    {
        var provider = A.Fake<ITerminalProvider>();
        A.CallTo(() => provider.SupportsMode(TerminalMode.VSCode)).Returns(false);
        A.CallTo(() => provider.IsAvailable).Returns(true);

        var service = CreateService([provider]);

        var result = service.OpenWorktree("/test/path", TerminalMode.VSCode);

        Assert.False(result);
        A.CallTo(() => provider.Open(A<string>._, A<TerminalMode>._, A<string?>._)).MustNotHaveHappened();
    }

    [Fact]
    public void OpenWorktree_SelectsHighestPriorityProvider()
    {
        var lowPriorityProvider = A.Fake<ITerminalProvider>();
        A.CallTo(() => lowPriorityProvider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => lowPriorityProvider.IsAvailable).Returns(true);
        A.CallTo(() => lowPriorityProvider.Priority).Returns(50);

        var highPriorityProvider = A.Fake<ITerminalProvider>();
        A.CallTo(() => highPriorityProvider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => highPriorityProvider.IsAvailable).Returns(true);
        A.CallTo(() => highPriorityProvider.Priority).Returns(150);
        A.CallTo(() => highPriorityProvider.Open(A<string>._, A<TerminalMode>._, A<string?>._)).Returns(true);

        var service = CreateService([lowPriorityProvider, highPriorityProvider]);

        var result = service.OpenWorktree("/test/path", TerminalMode.Tab);

        Assert.True(result);
        A.CallTo(() => highPriorityProvider.Open("/test/path", TerminalMode.Tab, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => lowPriorityProvider.Open(A<string>._, A<TerminalMode>._, A<string?>._)).MustNotHaveHappened();
    }

    [Fact]
    public void OpenWorktree_WithInitScripts_PassesCombinedInitCommand()
    {
        var provider = A.Fake<ITerminalProvider>();
        A.CallTo(() => provider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => provider.IsAvailable).Returns(true);
        A.CallTo(() => provider.Priority).Returns(100);
        A.CallTo(() => provider.Open(A<string>._, A<TerminalMode>._, A<string?>._)).Returns(true);

        var service = CreateService([provider]);

        service.OpenWorktree("/test/path", TerminalMode.Tab, "session-init", "after-init");

        A.CallTo(() => provider.Open("/test/path", TerminalMode.Tab, "session-init; after-init")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void OpenWorktree_WithOnlySessionInit_PassesSessionInitOnly()
    {
        var provider = A.Fake<ITerminalProvider>();
        A.CallTo(() => provider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => provider.IsAvailable).Returns(true);
        A.CallTo(() => provider.Priority).Returns(100);
        A.CallTo(() => provider.Open(A<string>._, A<TerminalMode>._, A<string?>._)).Returns(true);

        var service = CreateService([provider]);

        service.OpenWorktree("/test/path", TerminalMode.Tab, "session-init", null);

        A.CallTo(() => provider.Open("/test/path", TerminalMode.Tab, "session-init")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void OpenWorktree_WithOnlyAfterInit_PassesAfterInitOnly()
    {
        var provider = A.Fake<ITerminalProvider>();
        A.CallTo(() => provider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => provider.IsAvailable).Returns(true);
        A.CallTo(() => provider.Priority).Returns(100);
        A.CallTo(() => provider.Open(A<string>._, A<TerminalMode>._, A<string?>._)).Returns(true);

        var service = CreateService([provider]);

        service.OpenWorktree("/test/path", TerminalMode.Tab, null, "after-init");

        A.CallTo(() => provider.Open("/test/path", TerminalMode.Tab, "after-init")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void OpenWorktree_SkipsUnavailableHighPriorityProvider()
    {
        var unavailableHighPriority = A.Fake<ITerminalProvider>();
        A.CallTo(() => unavailableHighPriority.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => unavailableHighPriority.IsAvailable).Returns(false);
        A.CallTo(() => unavailableHighPriority.Priority).Returns(200);

        var availableLowPriority = A.Fake<ITerminalProvider>();
        A.CallTo(() => availableLowPriority.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => availableLowPriority.IsAvailable).Returns(true);
        A.CallTo(() => availableLowPriority.Priority).Returns(50);
        A.CallTo(() => availableLowPriority.Open(A<string>._, A<TerminalMode>._, A<string?>._)).Returns(true);

        var service = CreateService([unavailableHighPriority, availableLowPriority]);

        var result = service.OpenWorktree("/test/path", TerminalMode.Tab);

        Assert.True(result);
        A.CallTo(() => availableLowPriority.Open("/test/path", TerminalMode.Tab, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => unavailableHighPriority.Open(A<string>._, A<TerminalMode>._, A<string?>._)).MustNotHaveHappened();
    }

    [Fact]
    public void OpenWorktree_WithConfiguredProgram_SelectsMatchingProvider()
    {
        var defaultProvider = A.Fake<ITerminalProvider>();
        A.CallTo(() => defaultProvider.Name).Returns("Windows Terminal");
        A.CallTo(() => defaultProvider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => defaultProvider.IsAvailable).Returns(true);
        A.CallTo(() => defaultProvider.Priority).Returns(200);

        var wezTermProvider = A.Fake<ITerminalProvider>();
        A.CallTo(() => wezTermProvider.Name).Returns("WezTerm");
        A.CallTo(() => wezTermProvider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => wezTermProvider.IsAvailable).Returns(true);
        A.CallTo(() => wezTermProvider.Priority).Returns(50);
        A.CallTo(() => wezTermProvider.Open("/test/path", TerminalMode.Tab, null)).Returns(true);

        var service = CreateService(
            [defaultProvider, wezTermProvider],
            new TerminalConfig { Program = "wezterm" });

        var result = service.OpenWorktree("/test/path", TerminalMode.Tab);

        Assert.True(result);
        A.CallTo(() => wezTermProvider.Open("/test/path", TerminalMode.Tab, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => defaultProvider.Open(A<string>._, A<TerminalMode>._, A<string?>._)).MustNotHaveHappened();
    }

    [Fact]
    public void OpenWorktree_WithUnavailableConfiguredProgram_ReturnsFalse()
    {
        var wezTermProvider = A.Fake<ITerminalProvider>();
        A.CallTo(() => wezTermProvider.Name).Returns("WezTerm");
        A.CallTo(() => wezTermProvider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => wezTermProvider.IsAvailable).Returns(false);

        var fallbackProvider = A.Fake<ITerminalProvider>();
        A.CallTo(() => fallbackProvider.Name).Returns("Windows Terminal");
        A.CallTo(() => fallbackProvider.SupportsMode(TerminalMode.Tab)).Returns(true);
        A.CallTo(() => fallbackProvider.IsAvailable).Returns(true);

        var service = CreateService(
            [wezTermProvider, fallbackProvider],
            new TerminalConfig { Program = "wezterm" });

        var result = service.OpenWorktree("/test/path", TerminalMode.Tab);

        Assert.False(result);
        A.CallTo(() => fallbackProvider.Open(A<string>._, A<TerminalMode>._, A<string?>._)).MustNotHaveHappened();
    }
}
