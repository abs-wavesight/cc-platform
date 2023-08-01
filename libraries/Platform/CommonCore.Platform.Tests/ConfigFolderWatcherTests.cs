using Xunit;

namespace Abs.CommonCore.Platform.Tests
{
    public class ConfigFolderWatcherUnitTest : IDisposable
    {
        private readonly string _configFolderPath;
        private readonly string _existingConfigFilePath;
        private readonly ConfigFolderWatcher.ConfigFolderWatcher _configFolderWatcher;

        public ConfigFolderWatcherUnitTest()
        {
            _configFolderPath = GetTestFolderPath();
            _configFolderWatcher = CreateConfigFolderWatcher(_configFolderPath);
            _existingConfigFilePath = CreateNewConfigFile();
        }

        [Fact]
        public void AddedConfigFileTest()
        {
            string configWatcherResult = string.Empty;
            _configFolderWatcher.Added += (sender, filePath) =>
            {
                configWatcherResult = filePath;
            };

            var newFilePath = CreateNewConfigFile();

            Task.Delay(1000).Wait();

            Assert.Equal(newFilePath, configWatcherResult);
        }

        [Fact]
        public void AddedNotConfigFileTest()
        {
            string configWatcherResult = "";
            _configFolderWatcher.Added += (sender, filePath) =>
            {
                configWatcherResult = filePath;
            };

            var newFileName = $"{DateTime.Now.Ticks}.txt";
            var newFilePath = Path.Combine(_configFolderPath, newFileName);

            using (StreamWriter sw = File.CreateText(newFilePath))
            {
                sw.WriteLine("test new text file");
            }

            if (!File.Exists(newFilePath))
            {
                Assert.Fail($"The file {newFilePath} should exists here.");
            }

            Task.Delay(2000).Wait();

            Assert.Equal(string.Empty, configWatcherResult);
        }


        [Fact]
        public void ChangedConfigFileTest()
        {
            string configWatcherResult = string.Empty;
            _configFolderWatcher.Changed += (sender, filePath) =>
            {
                configWatcherResult = filePath;
            };

            if (File.Exists(_existingConfigFilePath))
            {
                File.WriteAllText(_existingConfigFilePath, "{ \"test\": \"1\"  }");
            }

            Task.Delay(1000).Wait();

            Assert.Equal(_existingConfigFilePath, configWatcherResult);
        }

        [Fact]
        public void DeletedConfigFileTest()
        {
            string configWatcherResult = string.Empty;
            _configFolderWatcher.Deleted += (sender, filePath) =>
            {
                configWatcherResult = filePath;
            };

            var filePathForDelete = CreateNewConfigFile();

            if (File.Exists(filePathForDelete))
            {
                File.Delete(filePathForDelete);
            }

            Task.Delay(1000).Wait();

            Assert.Equal(filePathForDelete, configWatcherResult);
        }

        private string GetTestFolderPath()
        {
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), $"TestFolderToWatch_{DateTime.Now.Ticks}");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return folderPath;
        }

        private ConfigFolderWatcher.ConfigFolderWatcher CreateConfigFolderWatcher(string configFolderPath)
        {
            var configFolderWatcher = new ConfigFolderWatcher.ConfigFolderWatcher(configFolderPath);

            return configFolderWatcher;
        }

        private string CreateNewConfigFile()
        {
            var newFileName = $"{DateTime.Now.Ticks}.json";
            var newFilePath = Path.Combine(_configFolderPath, newFileName);

            using (StreamWriter sw = File.CreateText(newFilePath))
            {
                sw.WriteLine("{}");
            }

            if (File.Exists(newFilePath))
            {
                return newFilePath;
            }

            return string.Empty;
        }

        public void Dispose()
        {
            if (Directory.Exists(_configFolderPath))
            {
                Directory.Delete(_configFolderPath, true);
            }
        }
    }
}
