using System.ComponentModel;
using Abs.CommonCore.Drex.Shared.Extensions;

namespace Abs.CommonCore.Drex.Shared.Tests.Extensions;
public class EnumExtensionsTests
{
    private const string DescriptionPrefix = "EnumDescription_";
    public enum TestEnum
    {
        [Description($"{DescriptionPrefix}A")]
        A,
        [Description($"{DescriptionPrefix}B")]
        B,
        [Description($"{DescriptionPrefix}C")]
        C,
        D
    }

    [Theory]
    [InlineData(TestEnum.A, $"{DescriptionPrefix}A")]
    [InlineData(TestEnum.B, $"{DescriptionPrefix}B")]
    [InlineData(TestEnum.C, $"{DescriptionPrefix}C")]
    [InlineData(TestEnum.D, "D")]
    public void GetDescription_TestEnumValue_CorrectDescription(TestEnum input, string? expectedResult)
    {
        // Act
        var result = input.GetDescription();

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
