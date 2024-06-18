namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher;

public class ConfigWatcherAddedTest : BaseConfigWatcherTest
{
    [Fact]
    public async Task GivenJsonFile_WhenFileAdded_ShouldFireAddedEvent()
    {
        // Arrange
        var configWatcherResult = string.Empty;
        ConfigFolderWatcher.Added += (_, filePath) => configWatcherResult = filePath;

        // Act
        var newFilePath = CreateNewConfigFile(ConfigFolderPath);

        await Task.Delay(DelayBetweenFileSystemOperations);

        // Assert
        Assert.Equal(newFilePath, configWatcherResult);
    }

    [Fact]
    public async Task GivenTextFile_WhenFileAdded_ShouldNotFireAddedEvent()
    {
        // Arrange
        var configWatcherResult = string.Empty;
        ConfigFolderWatcher.Added += (_, filePath) => configWatcherResult = filePath;

        var newFileName = $"{DateTime.Now.Ticks}.txt";
        var newFilePath = Path.Combine(ConfigFolderPath, newFileName);

        // Act
        await using (var sw = File.CreateText(newFilePath))
        {
            await sw.WriteLineAsync("test new text file");
        }

        if (!File.Exists(newFilePath))
        {
            Assert.Fail($"The file {newFilePath} should exists here.");
        }

        await Task.Delay(DelayBetweenFileSystemOperations);

        // Assert
        Assert.Equal(string.Empty, configWatcherResult);
    }
}
