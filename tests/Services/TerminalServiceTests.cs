using FakeItEasy;
using Hypr.Configuration;
using Hypr.Services;
using Hypr.Services.Terminals;
using Microsoft.Extensions.Logging;

namespace test.Services;

[Trait("Area", "Terminal")]
[Trait("Category", "Unit")]
public class TerminalServiceTests
{
    private readonly ILogger<TerminalService> _logger;

    public TerminalServiceTests()
    {
        _logger = A.Fake<ILogger<TerminalService>>();
    }

    [Fact]
    public void OpenWorktree_WithNoProviders_ReturnsFalse()
    {
        var service = new TerminalService(_logger, []);

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

        var service = new TerminalService(_logger, [provider]);

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

        var service = new TerminalService(_logger, [provider]);

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

        var service = new TerminalService(_logger, [provider]);

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

        var service = new TerminalService(_logger, [lowPriorityProvider, highPriorityProvider]);

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

        var service = new TerminalService(_logger, [provider]);

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

        var service = new TerminalService(_logger, [provider]);

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

        var service = new TerminalService(_logger, [provider]);

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

        var service = new TerminalService(_logger, [unavailableHighPriority, availableLowPriority]);

        var result = service.OpenWorktree("/test/path", TerminalMode.Tab);

        Assert.True(result);
        A.CallTo(() => availableLowPriority.Open("/test/path", TerminalMode.Tab, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => unavailableHighPriority.Open(A<string>._, A<TerminalMode>._, A<string?>._)).MustNotHaveHappened();
    }
}
