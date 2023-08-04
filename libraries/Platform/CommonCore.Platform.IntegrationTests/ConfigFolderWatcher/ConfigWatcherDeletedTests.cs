namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher
{
    public class ConfigFolderWatcherTest : BaseConfigWatcherTest
    {
        [Fact]
        public void IntegrationTest()
        {
            var configWatcherResult = string.Empty;
            ConfigFolderWatcher.Deleted += (_, filePath) =>
            {
                configWatcherResult = filePath;
            };

            var filePathForDelete = CreateNewConfigFile(ConfigFolderPath);

            if (File.Exists(filePathForDelete))
            {
                File.Delete(filePathForDelete);
            }

            Task.Delay(DelayBetweenFileSystemOperations).Wait();

            Assert.Equal(filePathForDelete, configWatcherResult);
        }
    }
}
