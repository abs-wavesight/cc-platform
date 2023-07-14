using Abs.CommonCore.LocalDevUtility.Commands.Configure;
using Abs.CommonCore.LocalDevUtility.Tests.Commands.Run;
using Abs.CommonCore.LocalDevUtility.Tests.Fixture;
using FluentAssertions;
using Xunit.Abstractions;

namespace Abs.CommonCore.LocalDevUtility.Tests.Commands.Configure;

[Collection(nameof(RunCommandTests))]
public class ConfigureCommandTests
{
    private readonly ITestOutputHelper _testOutput;

    public ConfigureCommandTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Fact]
    public void ValidateConfigAndThrow_GivenValidConfig_ShouldNotThrow()
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        var appConfig = fixture.GetValidTestConfig();

        // Act
        var exception = Record.Exception(() => ConfigureCommand.ValidateConfigAndThrow(appConfig));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void ValidateConfigAndThrow_GivenInvalidPlatformRepositoryPath_ShouldThrow()
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        var appConfig = fixture.GetValidTestConfig();
        appConfig.CommonCorePlatformRepositoryPath = "invalid";

        // Act
        var exception = Record.Exception(() => ConfigureCommand.ValidateConfigAndThrow(appConfig));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<Exception>();
        exception!.Message.Should().Contain("cc-platform");
    }

    [Fact]
    public void ValidateConfigAndThrow_GivenInvalidDrexRepositoryPath_ShouldThrow()
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        var appConfig = fixture.GetValidTestConfig();
        appConfig.CommonCoreDrexRepositoryPath = "invalid";

        // Act
        var exception = Record.Exception(() => ConfigureCommand.ValidateConfigAndThrow(appConfig));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<Exception>();
        exception!.Message.Should().Contain("cc-drex");
    }

    [Fact]
    public void ValidateConfigAndThrow_GivenInvalidContainerWindowsVersion_ShouldThrow()
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        var appConfig = fixture.GetValidTestConfig();
        appConfig.ContainerWindowsVersion = "invalid";

        // Act
        var exception = Record.Exception(() => ConfigureCommand.ValidateConfigAndThrow(appConfig));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<Exception>();
        exception!.Message.Should().Contain("Windows version");
    }

    [Fact]
    public void ValidateConfigAndThrow_GivenNullConfig_ShouldThrow()
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        AppConfig? appConfig = null;

        // Act
        var exception = Record.Exception(() => ConfigureCommand.ValidateConfigAndThrow(appConfig));

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<Exception>();
        exception!.Message.Should().Contain("not found");
    }

    [Fact]
    public void ReadConfig_GivenValidConfigFile_ShouldReturnAppConfigObject()
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        var expectedAppConfig = fixture.GetValidTestConfig();
        var fileName = ConfigureCommand.SaveConfig(expectedAppConfig).Result;
        File.Exists(fileName).Should().BeTrue();

        // Act
        var actualAppConfig = ConfigureCommand.ReadConfig().Result;

        // Assert
        actualAppConfig.Should().NotBeNull();
        actualAppConfig.Should().BeOfType<AppConfig>();
        actualAppConfig!.CommonCorePlatformRepositoryPath.Should().Be(expectedAppConfig.CommonCorePlatformRepositoryPath);
        actualAppConfig.CommonCoreDrexRepositoryPath.Should().Be(expectedAppConfig.CommonCoreDrexRepositoryPath);
        actualAppConfig.ContainerWindowsVersion.Should().Be(expectedAppConfig.ContainerWindowsVersion);
    }

    [Fact]
    public async Task ReadConfig_GivenNoExistingConfigFile_ShouldReturnNull()
    {
        // Arrange
        await ConfigureCommand.SaveConfig(null);

        // Act
        var actualAppConfig = ConfigureCommand.ReadConfig().Result;

        // Assert
        actualAppConfig.Should().BeNull();
    }

    [Fact]
    public void SaveConfig_GivenValidConfig_ShouldSaveConfigFile()
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        var appConfig = fixture.GetValidTestConfig();

        // Act
        var fileName = ConfigureCommand.SaveConfig(appConfig).Result;

        // Assert
        fileName.Should().NotBeNullOrEmpty();
        File.Exists(fileName).Should().BeTrue();
    }

    [Fact]
    public void SaveConfig_GivenNullConfigAndPreviouslyExistingConfigFile_ShouldDeletePreviouslyExistingConfigFile()
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        var appConfig = fixture.GetValidTestConfig();
        var originalFileName = ConfigureCommand.SaveConfig(appConfig).Result;
        File.Exists(originalFileName).Should().BeTrue();

        // Act
        var resultFileName = ConfigureCommand.SaveConfig(null).Result;

        // Assert
        resultFileName.Should().NotBeNullOrEmpty();
        File.Exists(originalFileName).Should().BeFalse();
        File.Exists(resultFileName).Should().BeFalse();
    }

    [Fact]
    public async Task ConfigureCommandTestX()
    {
        // TODO RH: Test Configure command
    }
}
