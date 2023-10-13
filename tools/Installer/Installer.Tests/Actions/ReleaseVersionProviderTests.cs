using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Moq;

namespace Installer.Tests.Actions;

public class ReleaseVersionProviderTests : TestsBase
{
    [Theory]
    [InlineData("RabbitMQ Release", new[] { "v100.0.11 Vector Release", "v100.0.10 RabbitMQ Release" }, "v100.0.10")]
    [InlineData("RabbitMQ Release", new[] { "v100.0.11 RabbitMQ Release", "v100.0.10 RabbitMQ Release" }, "v100.0.11")]
    [InlineData("Vector Release", new[] { "v100.0.11 RabbitMQ Release", "v100.0.10 RabbitMQ Release" }, null)]
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
