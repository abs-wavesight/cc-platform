﻿using Abs.CommonCore.Platform.ConfigFolderWatcher;
using Moq;
using Xunit;

namespace Abs.CommonCore.Platform.Tests.ConfigFolderWatcher;

public class ConfigFolderWatcherTests
{
    private readonly string _configFolderPath = "UnitTestFolderPath";
    private readonly string _existingConfigFilePath;
    private readonly IConfigFolderWatcher _configFolderWatcher;
    private readonly Mock<IConfigFolderWatcher> _configFolderWatcherMock;

    public ConfigFolderWatcherTests()
    {
        _configFolderWatcherMock = new Mock<IConfigFolderWatcher>();
        _configFolderWatcher = _configFolderWatcherMock.Object;

        _existingConfigFilePath = Path.Combine(_configFolderPath, $"{DateTime.Now.Ticks}.json");
    }

    [Fact]
    public void AddedConfigFileTest()
    {
        // Arrange
        var configWatcherResult = string.Empty;
        _configFolderWatcher.Added += (_, filePath) => configWatcherResult = filePath;

        var newFilePath = Path.Combine(_configFolderPath, $"{DateTime.Now.Ticks}.json");

        // Act
        _configFolderWatcherMock.Raise(w => w.Added += null, _configFolderWatcher, newFilePath);

        // Assert
        Assert.Equal(newFilePath, configWatcherResult);
    }

    [Fact]
    public void ChangedConfigFileTest()
    {
        // Arrange
        var configWatcherResult = string.Empty;
        _configFolderWatcher.Changed += (_, filePath) => configWatcherResult = filePath;

        // Act
        _configFolderWatcherMock.Raise(w => w.Changed += null, _configFolderWatcher, _existingConfigFilePath);

        // Assert
        Assert.Equal(_existingConfigFilePath, configWatcherResult);
    }

    [Fact]
    public void DeletedConfigFileTest()
    {
        // Arrange
        var configWatcherResult = string.Empty;
        _configFolderWatcher.Deleted += (_, filePath) => configWatcherResult = filePath;

        var filePathForDelete = Path.Combine(_configFolderPath, $"{DateTime.Now.Ticks}.json");

        // Act
        _configFolderWatcherMock.Raise(w => w.Deleted += null, _configFolderWatcher, filePathForDelete);

        // Assert
        Assert.Equal(filePathForDelete, configWatcherResult);
    }
}
