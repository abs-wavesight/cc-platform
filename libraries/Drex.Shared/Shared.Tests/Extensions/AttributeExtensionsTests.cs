using System.ComponentModel;
using Abs.CommonCore.Platform.Extensions;
using FluentAssertions;

namespace Abs.CommonCore.Drex.Shared.Tests.Extensions;

public class AttributeExtensionsTests
{
    [Fact]
    public void GetDescription_GivenDescriptionAttribute_ShouldReturnDescription()
    {
        // Act
        var result = typeof(DescriptionTestDummyClass).GetDescription(nameof(DescriptionTestDummyClass.PropOne));

        // Assert
        result.Should().Be("prop one");
    }

    [Fact]
    public void GetDescription_GivenNoDescriptionAttribute_ShouldReturnNull()
    {
        // Act
        var result = typeof(DescriptionTestDummyClass).GetDescription(nameof(DescriptionTestDummyClass.PropTwo));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDescription_GivenInvalidProperty_ShouldReturnNull()
    {
        // Act
        var result = typeof(DescriptionTestDummyClass).GetDescription("not a prop");

        // Assert
        result.Should().BeNull();
    }

    public class DescriptionTestDummyClass
    {
        [Description("prop one")]
        public string? PropOne { get; set; }

        public string? PropTwo { get; set; }
    }
}
