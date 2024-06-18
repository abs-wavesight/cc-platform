namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher;

public class ConfigFolderWatcherTest : BaseConfigWatcherTest
{
    [Fact]
    public async Task GivenJsonFile_WhenFileDeleted_ShouldFireDeletedEvent()
    {
        var configWatcherResult = string.Empty;
        ConfigFolderWatcher.Deleted += (_, filePath) => configWatcherResult = filePath;

        var filePathForDelete = CreateNewConfigFile(ConfigFolderPath);

        if (File.Exists(filePathForDelete))
        {
            File.Delete(filePathForDelete);
        }

        await Task.Delay(DelayBetweenFileSystemOperations);

        Assert.Equal(filePathForDelete, configWatcherResult);
    }
}
