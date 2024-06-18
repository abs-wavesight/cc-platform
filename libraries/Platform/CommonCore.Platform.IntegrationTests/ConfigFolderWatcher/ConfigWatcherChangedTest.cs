namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher;

public class ConfigWatcherChangedTest : BaseConfigWatcherTest
{
    private readonly string _existingConfigFilePath;

    public ConfigWatcherChangedTest()
    {
        _existingConfigFilePath = CreateNewConfigFile(ConfigFolderPath);
        Task.Delay(DelayBetweenFileSystemOperations).Wait();
    }

    [Fact]
    public async Task GivenJsonFile_WhenFileChanged_ShouldFireChangedEvent()
    {
        // Arrange
        var configWatcherResult = new List<string>();
        var shouldBeEmptyWatcherResult = string.Empty;
        ConfigFolderWatcher.Changed += (_, filePath) => configWatcherResult.Add(filePath);
        ConfigFolderWatcher.Added += (_, filePath) => shouldBeEmptyWatcherResult = filePath;

        // Act
        if (File.Exists(_existingConfigFilePath))
        {
            await File.WriteAllTextAsync(_existingConfigFilePath, $"{{ \"test\": {DateTime.Now.Ticks} }}");
        }

        await Task.Delay(DelayBetweenFileSystemOperations);

        // Assert
        Assert.True(configWatcherResult.Any());
        Assert.Equal(_existingConfigFilePath, configWatcherResult[0]);
        Assert.Equal(string.Empty, shouldBeEmptyWatcherResult);
    }
}
