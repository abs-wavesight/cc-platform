namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher;

public class ConfigWatcherAddedTest : BaseConfigWatcherTest
{
    [Fact]
    public void GivenJsonFile_WhenFileAdded_ShouldFireAddedEvent()
    {
        // Arrange
        var configWatcherResult = string.Empty;
        ConfigFolderWatcher.Added += (_, filePath) => configWatcherResult = filePath;

        // Act
        var newFilePath = CreateNewConfigFile(ConfigFolderPath);

        Task.Delay(DelayBetweenFileSystemOperations).Wait();

        // Assert
        Assert.Equal(newFilePath, configWatcherResult);
    }

    [Fact]
    public void GivenTextFile_WhenFileAdded_ShouldNotFireAddedEvent()
    {
        // Arrange
        var configWatcherResult = string.Empty;
        ConfigFolderWatcher.Added += (_, filePath) => configWatcherResult = filePath;

        var newFileName = $"{DateTime.Now.Ticks}.txt";
        var newFilePath = Path.Combine(ConfigFolderPath, newFileName);

        // Act
        using (var sw = File.CreateText(newFilePath))
        {
            sw.WriteLine("test new text file");
        }

        if (!File.Exists(newFilePath))
        {
            Assert.Fail($"The file {newFilePath} should exists here.");
        }

        Task.Delay(DelayBetweenFileSystemOperations).Wait();

        // Assert
        Assert.Equal(string.Empty, configWatcherResult);
    }
}
