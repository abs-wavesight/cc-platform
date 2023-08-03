using System.Reflection;
using Abs.CommonCore.Platform.ConfigFolderWatcher;
using Xunit.Abstractions;

namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher
{
    public class ConfigFolderWatcherTest : IDisposable
    {
        private readonly ITestOutputHelper _output;

        private readonly string _configFolderPath;
        private readonly IConfigFolderWatcher _configFolderWatcher;
        private readonly int _delay = 500;

        public ConfigFolderWatcherTest(ITestOutputHelper output)
        {
            _output = output;

            _configFolderPath = GetTestFolderPath();
            _configFolderWatcher = new Platform.ConfigFolderWatcher.ConfigFolderWatcher(_configFolderPath);

            _output.WriteLine($"Folder to watch {_configFolderPath}");
        }

        [Fact]
        public void AddedConfigFileTest()
        {
            // Arrange
            var configWatcherResult = string.Empty;
            _configFolderWatcher.Added += (_, filePath) =>
            {
                configWatcherResult = filePath;
            };

            // Act
            var newFilePath = CreateNewConfigFile(_configFolderPath);

            Task.Delay(_delay).Wait();

            // Assert
            Assert.Equal(newFilePath, configWatcherResult);
        }

        [Fact]
        public void AddedNotConfigFileTest()
        {
            // Arrange
            var configWatcherResult = string.Empty;
            _configFolderWatcher.Added += (_, filePath) =>
            {
                configWatcherResult = filePath;
            };

            var newFileName = $"{DateTime.Now.Ticks}.txt";
            var newFilePath = Path.Combine(_configFolderPath, newFileName);

            // Act
            using (StreamWriter sw = File.CreateText(newFilePath))
            {
                sw.WriteLine("test new text file");
            }

            if (!File.Exists(newFilePath))
            {
                Assert.Fail($"The file {newFilePath} should exists here.");
            }

            Task.Delay(_delay).Wait();

            // Assert
            Assert.Equal(string.Empty, configWatcherResult);
        }

        /*
        [Fact]
        public void ChangedConfigFileTest()
        {
            // Arrange
            var existingConfigFilePath = CreateNewConfigFile(_configFolderPath);
            Task.Delay(500).Wait();

            _output.WriteLine($"Existing File: {existingConfigFilePath}");
            var configWatcherResult = string.Empty;
            var shouldBeEmptyWatcherResult = string.Empty;
            _configFolderWatcher.Changed += (_, filePath) =>
            {
                _output.WriteLine($"Change {filePath}");
                configWatcherResult = filePath;
            };
            _configFolderWatcher.Added += (_, filePath) =>
            {
                _output.WriteLine($"Added {filePath}");
                shouldBeEmptyWatcherResult = filePath;
            };

            // Act
            if (File.Exists(existingConfigFilePath))
            {
                _output.WriteLine($"Write to {existingConfigFilePath}");
                File.WriteAllText(existingConfigFilePath, $"{{ \"test\": {DateTime.Now.Ticks} }}");
            }

            Task.Delay(_delay).Wait();

            // Assert
            Assert.Equal(existingConfigFilePath, configWatcherResult);
            Assert.Equal(string.Empty, shouldBeEmptyWatcherResult);
        }
        */

        [Fact]
        public void DeletedConfigFileTest()
        {
            var configWatcherResult = string.Empty;
            _configFolderWatcher.Deleted += (_, filePath) =>
            {
                configWatcherResult = filePath;
            };

            var filePathForDelete = CreateNewConfigFile(_configFolderPath);

            if (File.Exists(filePathForDelete))
            {
                File.Delete(filePathForDelete);
            }

            Task.Delay(_delay).Wait();

            Assert.Equal(filePathForDelete, configWatcherResult);
        }

        private string GetTestFolderPath()
        {
            var currentDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            //var testFolderPath = Path.Combine(currentDirectoryPath, "IntegrationTestsRuntimeData");
            var testFolderPath = currentDirectoryPath;

            if (!Directory.Exists(testFolderPath))
            {
                throw new DirectoryNotFoundException(testFolderPath);
            }

            var folderPath = Path.Combine(testFolderPath, $"TestFolderToWatch_{DateTime.Now.Ticks}");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return folderPath;
        }

        private static string CreateNewConfigFile(string configFolderPath, string filePrefix = "")
        {
            var newFileName = $"{filePrefix}{DateTime.Now.Ticks}.json";
            var newFilePath = Path.Combine(configFolderPath, newFileName);

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
            _configFolderWatcher.Dispose();

            if (Directory.Exists(_configFolderPath))
            {
                Directory.Delete(_configFolderPath, true);
            }
        }
    }
}
