using System.ComponentModel;
using Abs.CommonCore.Platform.Extensions;
using FluentAssertions;
using Xunit;

namespace Abs.CommonCore.Platform.Tests.Extensions;

public class AttributeExtensionsTests
{
    private const string DescriptionPrefix = "EnumDescription_";

    [Theory]
    [InlineData(TestEnum.A, $"{DescriptionPrefix}A")]
    [InlineData(TestEnum.B, $"{DescriptionPrefix}B")]
    [InlineData(TestEnum.X, null)]
    public void GetDescription_TestEnumValue_CorrectDescription(TestEnum input, string? expectedResult)
    {
        // Act
        var result = input.GetDescription();

        // Assert
        Assert.Equal(expectedResult, result);
    }

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

    public enum TestEnum
    {
        [Description($"{DescriptionPrefix}A")]
        A,
        [Description($"{DescriptionPrefix}B")]
        B,
        X
    }
}
