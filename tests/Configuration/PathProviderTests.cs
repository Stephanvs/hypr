using System.Runtime.InteropServices;
using FluentAssertions;
using Hyprwt.Configuration;

namespace test.Configuration;

public class PathProviderTests
{
  [Trait("Platform", "Linux")]
  [Fact]
  public void PathProvider_OnLinux_GetGlobalConfigPath_ReturnsCorrectPath()
  {
    Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "tests requires linux");
    PathProvider.GetGlobalConfigPath().Should().Be("/home/stephanvs/.config/hyprwt/config.json");
  }

  [Trait("Platform", "macOS")]
  [Fact]
  public void PathProvider_OnMacOS_GetGlobalConfigPath_ReturnsCorrectPath()
  {
    Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), "tests requires macOS");
    PathProvider.GetGlobalConfigPath().Should().Be("/home/stephanvs/.config/hyprwt/config.json");
  }

  [Trait("Platform", "Windows")]
  [Fact]
  public void PathProvider_OnWindows_GetGlobalConfigPath_ReturnsCorrectPath()
  {
    Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "tests requires Windows");
    PathProvider.GetGlobalConfigPath().ToLower().Should().Be("c:/users/stephanvs/hyprwt/config.json");
  }
}