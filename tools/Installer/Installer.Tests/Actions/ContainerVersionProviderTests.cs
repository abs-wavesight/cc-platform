using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Moq;

namespace Installer.Tests.Actions;

public class ContainerVersionProviderTests : TestsBase
{
    [Theory]
    [InlineData(new[] { "windows-2019-main-absc123", "windows-2019-main-absc124", "foo" }, "absc123")]
    [InlineData(new[]
    {
        "windows-2022-main-abcdefgg", // Not 7 characters
        "windows-2016-main-abcd123",  // Wrong year
        "Windows-2019-Main-1aB2c3D"// Case insensitive
    }, "1aB2c3D")]

    public async Task GetLatestContainerVersionAsync(IEnumerable<string> tags, string expectedResult)
    {
        // Arrange
        var releaseDataProvider = new Mock<IContainerTagProvider>();
        releaseDataProvider
            .Setup(x => x.GetContainerTagsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(tags));
        var testObj = new ContainerVersionProvider(releaseDataProvider.Object);

        // Act
        var result = await testObj.GetLatestContainerVersionAsync(string.Empty, string.Empty);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
