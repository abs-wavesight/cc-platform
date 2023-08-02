//#define LOCAL_DEBUG
#if LOCAL_DEBUG
using FS = System.IO;
#else
using FS = Abs.CommonCore.Platform.Tests.Mocks;
#endif
using System.Reflection;
using NSubstitute;
using Xunit;

namespace Abs.CommonCore.Platform.Tests
{
    public class ConfigFolderWatcherUnitTest : IDisposable
    {
        private readonly string _configFolderPath;
        private readonly string _existingConfigFilePath;
        private readonly ConfigFolderWatcher.ICustomFileSystemWatcher _configFolderWatcher;
#if LOCAL_DEBUG
        private readonly bool _localDebug = true;
#else
        private readonly bool _localDebug = false;
#endif

        public ConfigFolderWatcherUnitTest()
        {
            _configFolderPath = GetTestFolderPath();
            if (_localDebug)
            {
                _configFolderWatcher = CreateConfigFolderWatcher(_configFolderPath);
            }
            else
            {
                _configFolderWatcher = CreateConfigFolderWatcherForUnitTest();
            }

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

            if (!_localDebug)
            {
                _configFolderWatcher.Added += Raise.Event<EventHandler<string>>(_configFolderWatcher, newFilePath);
            }
            else
            {
                Task.Delay(1000).Wait();
            }
            
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

            using (FS.StreamWriter sw = FS.File.CreateText(newFilePath))
            {
                sw.WriteLine("test new text file");
            }

            if (!FS.File.Exists(newFilePath))
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

            if (FS.File.Exists(_existingConfigFilePath))
            {
                FS.File.WriteAllText(_existingConfigFilePath, "{ \"test\": \"1\"  }");
            }

            if (!_localDebug)
            {
                _configFolderWatcher.Changed += Raise.Event<EventHandler<string>>(_configFolderWatcher, _existingConfigFilePath);
            }
            else
            {
                Task.Delay(1000).Wait();
            }

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

            if (FS.File.Exists(filePathForDelete))
            {
                FS.File.Delete(filePathForDelete);
            }

            if (!_localDebug)
            {
                _configFolderWatcher.Deleted += Raise.Event<EventHandler<string>>(_configFolderWatcher, filePathForDelete);
            }
            else
            {
                Task.Delay(1000).Wait();
            }

            Assert.Equal(filePathForDelete, configWatcherResult);
        }

        private string GetTestFolderPath()
        {
            var currentDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            var folderPath = Path.Combine(currentDirectoryPath, $"TestFolderToWatch_{DateTime.Now.Ticks}");

            if (!FS.Directory.Exists(folderPath))
            {
                FS.Directory.CreateDirectory(folderPath);
            }

            return folderPath;
        }

        private ConfigFolderWatcher.ICustomFileSystemWatcher CreateConfigFolderWatcher(string configFolderPath)
        {
            var configFolderWatcher = new ConfigFolderWatcher.ConfigFolderWatcher(configFolderPath);

            return configFolderWatcher;
        }

        private ConfigFolderWatcher.ICustomFileSystemWatcher CreateConfigFolderWatcherForUnitTest()
        {
            var configFolderWatcher = Substitute.For<ConfigFolderWatcher.ICustomFileSystemWatcher>();

            return configFolderWatcher;
        }

        private string CreateNewConfigFile()
        {
            var newFileName = $"{DateTime.Now.Ticks}.json";
            var newFilePath = Path.Combine(_configFolderPath, newFileName);

            if (_localDebug)
            {
                using (FS.StreamWriter sw = FS.File.CreateText(newFilePath))
                {
                    sw.WriteLine("{}");
                }

                if (FS.File.Exists(newFilePath))
                {
                    return newFilePath;
                }
            }
            else
            {
                return newFilePath;
            }

            return string.Empty;
        }

        public void Dispose()
        {
            if (FS.Directory.Exists(_configFolderPath))
            {
                FS.Directory.Delete(_configFolderPath, true);
            }
        }
    }
}
