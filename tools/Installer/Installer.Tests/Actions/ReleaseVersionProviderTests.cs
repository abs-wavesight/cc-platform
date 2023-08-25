using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Moq;

namespace Installer.Tests.Actions;

public class ReleaseVersionProviderTests : TestsBase
{
    [Theory]
    [InlineData("RabbitMQ Release 2019", new[] { "RabbitMQ Release 2022 v100.0.11", "RabbitMQ Release 2019 v100.0.10" }, "v100.0.10")]
    [InlineData("RabbitMQ Release 2022", new[] { "RabbitMQ Release 2022 v100.0.11", "RabbitMQ Release 2019 v100.0.10" }, "v100.0.11")]
    [InlineData("RabbitMQ Release 2025", new[] { "RabbitMQ Release 2022 v100.0.11", "RabbitMQ Release 2019 v100.0.10" }, null)]
    public async Task GetVersionAsync_GivenCollection_ReturnsExpected(string releaseName, IEnumerable<string> releaseNames, string? expectedResult)
    {
        // Arrange
        var releaseDataProvider = new Mock<IReleaseDataProvider>();
        releaseDataProvider
            .Setup(x => x.GetReleaseNames(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(releaseNames));
        var testObj = new ReleaseVersionProvider(releaseDataProvider.Object);

        // Act
        var result = await testObj.GetVersionAsync(releaseName, string.Empty, string.Empty);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
