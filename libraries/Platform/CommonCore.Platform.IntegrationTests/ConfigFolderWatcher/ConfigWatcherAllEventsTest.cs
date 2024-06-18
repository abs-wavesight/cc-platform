namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher;

public class ConfigWatcherAllEventsTest : BaseConfigWatcherTest
{
    private readonly string _existingConfigFilePath;
    private string _shouldBeEmptyWatcherChangedResult = string.Empty;
    private string _configWatcherAddedResult = string.Empty;
    private string _configWatcherDeletedResult = string.Empty;

    public ConfigWatcherAllEventsTest()
    {
        _existingConfigFilePath = CreateNewConfigFile(ConfigFolderPath);
        Task.Delay(DelayBetweenFileSystemOperations).Wait();
    }

    [Fact]
    public async Task FireAllEventsUsingOneConfigWatcherInstance()
    {
        // Arrange
        List<string> configWatcherChangedResult = new();
        AutoResetEvent changeEvent = new(true);
        ConfigFolderWatcher.Changed += (_, filePath) =>
        {
            configWatcherChangedResult.Add(filePath);
            changeEvent.Set();
        };
        ConfigFolderWatcher.Added += (_, filePath) =>
        {
            _configWatcherAddedResult = filePath;
            _shouldBeEmptyWatcherChangedResult = filePath;
        };
        ConfigFolderWatcher.Deleted += (_, filePath) => _configWatcherDeletedResult = filePath;

        // Act Assert
        await ActAssertClearChangedEvent(configWatcherChangedResult, changeEvent);

        await ActAssertClearAddedEvent(configWatcherChangedResult);

        await ActAssertClearChangedEvent(configWatcherChangedResult, changeEvent);

        await ActAssertClearChangedEvent(configWatcherChangedResult, changeEvent);

        await ActAssertClearDeletedEvent();
    }

    private async Task ActAssertClearDeletedEvent()
    {
        // Act
        var filePathForDelete = CreateNewConfigFile(ConfigFolderPath);

        if (File.Exists(filePathForDelete))
        {
            File.Delete(filePathForDelete);
        }

        await Task.Delay(DelayBetweenFileSystemOperations);

        // Assert
        Assert.Equal(filePathForDelete, _configWatcherDeletedResult);
    }

    private async Task ActAssertClearAddedEvent(List<string> configWatcherChangedResult)
    {
        // Act
        var newFilePath = CreateNewConfigFile(ConfigFolderPath);

        await Task.Delay(DelayBetweenFileSystemOperations);

        // Assert
        Assert.Equal(newFilePath, _configWatcherAddedResult);
        Assert.Empty(configWatcherChangedResult);

        // Clear
        _shouldBeEmptyWatcherChangedResult = string.Empty;
    }

    private async Task ActAssertClearChangedEvent(List<string> configWatcherChangedResult,
        AutoResetEvent changeEvent)
    {
        // Act
        if (File.Exists(_existingConfigFilePath))
        {
            await File.WriteAllTextAsync(_existingConfigFilePath, $"{{ \"test\": {DateTime.Now.Ticks} }}");
        }

        if (!changeEvent.WaitOne(5 * DelayBetweenFileSystemOperations))
        {
            Assert.Fail("Change wasn't handled on time.");
        }

        // Assert
        Assert.True(configWatcherChangedResult.Any());
        Assert.Equal(_existingConfigFilePath, configWatcherChangedResult[0]);
        Assert.Equal(string.Empty, _shouldBeEmptyWatcherChangedResult);

        // Clear
        configWatcherChangedResult.Clear();
    }
}
