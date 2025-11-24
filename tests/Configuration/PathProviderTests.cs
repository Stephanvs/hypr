using FluentAssertions;
using Hyprwt.Configuration;

namespace test.Configuration;

public class PathProviderTests
{
  [Trait("Platform", "Linux")]
  [Fact]
  public void PathProvider_OnLinux_GetGlobalConfigPath_ReturnsCorrectPath()
  {
    Assert.SkipUnless(OperatingSystem.IsLinux(), "tests requires linux");
    var expectedPath = Path.Combine("/home", Environment.UserName, ".config", "hyprwt", "config.json");
    PathProvider.GetGlobalConfigPath().Should().Be(expectedPath);
  }

  [Trait("Platform", "macOS")]
  [Fact]
  public void PathProvider_OnMacOS_GetGlobalConfigPath_ReturnsCorrectPath()
  {
    Assert.SkipUnless(OperatingSystem.IsMacOS(), "tests requires macOS");
    var expectedPath = Path.Combine("/Users", Environment.UserName, "Library", "Application Support", "hyprwt", "config.json");
    PathProvider.GetGlobalConfigPath().Should().Be(expectedPath);
  }

  [Trait("Platform", "Windows")]
  [Fact]
  public void PathProvider_OnWindows_GetGlobalConfigPath_ReturnsCorrectPath()
  {
    Assert.SkipUnless(OperatingSystem.IsWindows(), "tests requires Windows");
    var expectedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "hyprwt", "config.json");
    PathProvider.GetGlobalConfigPath().ToLower().Should().Be(expectedPath.ToLower());
  }
}