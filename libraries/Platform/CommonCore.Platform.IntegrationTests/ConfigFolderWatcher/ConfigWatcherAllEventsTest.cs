namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher;

public class ConfigWatcherAllEventsTest : BaseConfigWatcherTest
{
    private readonly string _existingConfigFilePath;
    private readonly List<string> _configWatcherChangedResult = new();
    private string _shouldBeEmptyWatcherChangedResult = string.Empty;
    private string _configWatcherAddedResult = string.Empty;
    private string _configWatcherDeletedResult = string.Empty;

    private readonly AutoResetEvent _changeEvent = new(true);

    public ConfigWatcherAllEventsTest()
    {
        _existingConfigFilePath = CreateNewConfigFile(ConfigFolderPath);
        Task.Delay(DelayBetweenFileSystemOperations).Wait();
    }

    [Fact]
    public async Task FireAllEventsUsingOneConfigWatcherInstance()
    {
        // Arrange
        ConfigFolderWatcher.Changed += (_, filePath) =>
        {
            _configWatcherChangedResult.Add(filePath);
            _changeEvent.Set();
        };
        ConfigFolderWatcher.Added += (_, filePath) =>
        {
            _configWatcherAddedResult = filePath;
            _shouldBeEmptyWatcherChangedResult = filePath;
        };
        ConfigFolderWatcher.Deleted += (_, filePath) => _configWatcherDeletedResult = filePath;

        // Act Assert
        await ActAssertClearChangedEvent();

        await ActAssertClearAddedEvent();

        await ActAssertClearChangedEvent();

        await ActAssertClearChangedEvent();

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

    private async Task ActAssertClearAddedEvent()
    {
        // Act
        var newFilePath = CreateNewConfigFile(ConfigFolderPath);

        await Task.Delay(DelayBetweenFileSystemOperations);

        // Assert
        Assert.Equal(newFilePath, _configWatcherAddedResult);
        Assert.Empty(_configWatcherChangedResult);

        // Clear
        _shouldBeEmptyWatcherChangedResult = string.Empty;
    }

    private async Task ActAssertClearChangedEvent()
    {
        // Act
        if (File.Exists(_existingConfigFilePath))
        {
            await File.WriteAllTextAsync(_existingConfigFilePath, $"{{ \"test\": {DateTime.Now.Ticks} }}");
        }

        if (!_changeEvent.WaitOne(5 * DelayBetweenFileSystemOperations))
        {
            Assert.Fail("Change wasn't handled on time.");
        }

        // Assert
        Assert.True(_configWatcherChangedResult.Any());
        Assert.Equal(_existingConfigFilePath, _configWatcherChangedResult[0]);
        Assert.Equal(string.Empty, _shouldBeEmptyWatcherChangedResult);

        // Clear
        _configWatcherChangedResult.Clear();
    }
}
