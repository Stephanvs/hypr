using FluentAssertions;
using Xunit;

namespace Hyprwt.Tests;

/// <summary>
/// Basic tests to verify project setup.
/// </summary>
public class ProgramTests
{
    [Fact]
    public void BasicTest_ShouldPass()
    {
        // Arrange & Act
        var result = 1 + 1;

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task Main_ShouldReturnZero()
    {
        // Arrange
        string[] args = Array.Empty<string>();

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        exitCode.Should().Be(0);
    }
}
