using System.Reflection;
using Abs.CommonCore.Platform.ConfigFolderWatcher;
using Xunit.Abstractions;

namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher
{
    public class ConfigChangedTest : IDisposable
    {
        private readonly ITestOutputHelper _output;

        private readonly string _configFolderPath;
        private readonly IConfigFolderWatcher _configFolderWatcher;
        private readonly int _delayBetweenFileSystemOperations = 1000;

        private readonly string _existingConfigFilePath;

        public ConfigChangedTest(ITestOutputHelper output)
        {
            _output = output;

            _configFolderPath = GetTestFolderPath();
            _configFolderWatcher = new Platform.ConfigFolderWatcher.ConfigFolderWatcher(_configFolderPath);

            _existingConfigFilePath = CreateNewConfigFile(_configFolderPath);
            Task.Delay(_delayBetweenFileSystemOperations).Wait();

            _output.WriteLine($"Folder to watch {_configFolderPath}");
        }

        [Fact]
        public void ChangedConfigFileTest()
        {
            // Arrange
            _output.WriteLine($"Existing File: {_existingConfigFilePath}");
            var configWatcherResult = new List<string>();
            var shouldBeEmptyWatcherResult = string.Empty;
            _configFolderWatcher.Changed += (_, filePath) =>
            {
                _output.WriteLine($"Change {filePath}");
                configWatcherResult.Add(filePath);
            };
            _configFolderWatcher.Added += (_, filePath) =>
            {
                _output.WriteLine($"Added {filePath}");
                shouldBeEmptyWatcherResult = filePath;
            };

            // Act
            if (File.Exists(_existingConfigFilePath))
            {
                _output.WriteLine($"Write to {_existingConfigFilePath}");
                File.WriteAllText(_existingConfigFilePath, $"{{ \"test\": {DateTime.Now.Ticks} }}");
            }

            Task.Delay(_delayBetweenFileSystemOperations).Wait();

            // Assert
            Assert.Single(configWatcherResult);
            Assert.Equal(_existingConfigFilePath, configWatcherResult[0]);
            Assert.Equal(string.Empty, shouldBeEmptyWatcherResult);
        }

        private string GetTestFolderPath()
        {
            var currentDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            var testFolderPath = Path.Combine(currentDirectoryPath, "IntegrationTestsRuntimeData");
            
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
