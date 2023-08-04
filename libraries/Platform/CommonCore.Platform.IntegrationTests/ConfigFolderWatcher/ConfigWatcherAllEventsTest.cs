namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher
{
    public class ConfigWatcherAllEventsTest : BaseConfigWatcherTest
    {
        private readonly string _existingConfigFilePath;

        private string _shouldBeEmptyWatcherChangedResult = string.Empty;
        private readonly List<string> _configWatcherChangedResult = new();
        private string _configWatcherAddedResult = string.Empty;
        private string _configWatcherDeletedResult = string.Empty;

        public ConfigWatcherAllEventsTest()
        {
            _existingConfigFilePath = CreateNewConfigFile(ConfigFolderPath);
            Task.Delay(DelayBetweenFileSystemOperations).Wait();
        }

        [Fact]
        public void FireAllEventsUsingOneConfigWatcherInstance()
        {
            // Arrange
            ConfigFolderWatcher.Changed += (_, filePath) =>
            {
                _configWatcherChangedResult.Add(filePath);
            };
            ConfigFolderWatcher.Added += (_, filePath) =>
            {
                _configWatcherAddedResult = filePath;
                _shouldBeEmptyWatcherChangedResult = filePath;
            };
            ConfigFolderWatcher.Deleted += (_, filePath) =>
            {
                _configWatcherDeletedResult = filePath;
            };

            // Act Assert
            ActAssertClearChangedEvent();

            ActAssertClearAddedEvent();

            ActAssertClearChangedEvent();

            ActAssertClearChangedEvent();
            
            ActAssertClearDeletedEvent();
        }

        private void ActAssertClearDeletedEvent()
        {
            // Act
            var filePathForDelete = CreateNewConfigFile(ConfigFolderPath);

            if (File.Exists(filePathForDelete))
            {
                File.Delete(filePathForDelete);
            }

            Task.Delay(DelayBetweenFileSystemOperations).Wait();

            // Assert
            Assert.Equal(filePathForDelete, _configWatcherDeletedResult);
        }

        private void ActAssertClearAddedEvent()
        {
            // Act
            var newFilePath = CreateNewConfigFile(ConfigFolderPath);

            Task.Delay(DelayBetweenFileSystemOperations).Wait();

            // Assert
            Assert.Equal(newFilePath, _configWatcherAddedResult);
            Assert.Empty(_configWatcherChangedResult);

            // Clear
            _shouldBeEmptyWatcherChangedResult = string.Empty;
        }

        private void ActAssertClearChangedEvent()
        {
            // Act
            if (File.Exists(_existingConfigFilePath))
            {
                File.WriteAllText(_existingConfigFilePath, $"{{ \"test\": {DateTime.Now.Ticks} }}");
            }

            Task.Delay(DelayBetweenFileSystemOperations).Wait();

            // Assert
            Assert.Single(_configWatcherChangedResult);
            Assert.Equal(_existingConfigFilePath, _configWatcherChangedResult[0]);
            Assert.Equal(string.Empty, _shouldBeEmptyWatcherChangedResult);

            // Clear
            _configWatcherChangedResult.Clear();
        }
    }
}
