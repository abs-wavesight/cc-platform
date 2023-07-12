using Abs.CommonCore.Platform.Extensions;
using FluentAssertions;
using Xunit;

namespace Abs.CommonCore.Platform.Tests.Extensions
{
    public class ListExtensionsTests
    {
        [Fact]
        public void AddIfNotNullOrEmpty_GivenNotNullOrEmpty_ShouldAdd()
        {
            // Arrange
            const string value = "the value";
            var list = new List<string>();

            // Act
            list.AddIfNotNullOrEmpty(value);

            // Assert
            list.Should().HaveCount(1);
            list.First().Should().Be(value);
        }

        [Fact]
        public void AddIfNotNullOrEmpty_GivenNull_ShouldNotAdd()
        {
            // Arrange
            const string? value = null;
            var list = new List<string>();

            // Act
            list.AddIfNotNullOrEmpty(value);

            // Assert
            list.Should().HaveCount(0);
        }

        [Fact]
        public void AddIfNotNullOrEmpty_GivenEmpty_ShouldNotAdd()
        {
            // Arrange
            const string value = "";
            var list = new List<string>();

            // Act
            list.AddIfNotNullOrEmpty(value);

            // Assert
            list.Should().HaveCount(0);
        }
    }
}
