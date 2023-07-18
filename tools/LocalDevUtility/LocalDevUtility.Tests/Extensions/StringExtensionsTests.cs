using Abs.CommonCore.LocalDevUtility.Extensions;
using FluentAssertions;

namespace Abs.CommonCore.LocalDevUtility.Tests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void ToSnakeCase_GivenPascalCase_ShouldReturnSnakeCase()
    {
        // Arrange
        var str = "TestString";

        // Act
        var result = str.ToSnakeCase();

        // Assert
        result.Should().Be("test-string");
    }

    [Fact]
    public void TrimTrailingSlash_GivenTrailingForwardSlash_ShouldTrim()
    {
        // Arrange
        var str = "test/";

        // Act
        var result = str.TrimTrailingSlash();

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void TrimTrailingSlash_GivenTrailingBackSlash_ShouldTrim()
    {
        // Arrange
        var str = "test\\";

        // Act
        var result = str.TrimTrailingSlash();

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void TrimTrailingSlash_GivenNoTrailingSlash_ShouldReturnOriginalString()
    {
        // Arrange
        var str = "test";

        // Act
        var result = str.TrimTrailingSlash();

        // Assert
        result.Should().Be("test");
    }
}
