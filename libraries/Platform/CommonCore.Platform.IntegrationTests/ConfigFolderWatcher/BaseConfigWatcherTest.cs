﻿using System.Reflection;
using Abs.CommonCore.Platform.ConfigFolderWatcher;

namespace Abs.CommonCore.Platform.IntegrationTests.ConfigFolderWatcher;

public abstract class BaseConfigWatcherTest : IDisposable
{
    protected readonly string ConfigFolderPath;
    protected readonly IConfigFolderWatcher ConfigFolderWatcher;
    protected readonly int DelayBetweenFileSystemOperations = 1000;

    protected BaseConfigWatcherTest()
    {
        ConfigFolderPath = GetTestFolderPath();
        ConfigFolderWatcher = new Platform.ConfigFolderWatcher.ConfigFolderWatcher(ConfigFolderPath);
    }

    private static string GetTestFolderPath()
    {
        var currentDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        var testFolderPath = Path.Combine(currentDirectoryPath, "IntegrationTestsRuntimeData");

        if (!Directory.Exists(testFolderPath))
        {
            throw new DirectoryNotFoundException(testFolderPath);
        }

        var folderPath = Path.Combine(testFolderPath, $"TestFolderToWatch_{Guid.NewGuid():N}");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return folderPath;
    }

    protected static string CreateNewConfigFile(string configFolderPath, string filePrefix = "")
    {
        var newFileName = $"{filePrefix}{DateTime.Now.Ticks}.json";
        var newFilePath = Path.Combine(configFolderPath, newFileName);

        using (var sw = File.CreateText(newFilePath))
        {
            sw.WriteLine("{}");
        }

        return File.Exists(newFilePath) ? newFilePath : string.Empty;
    }

    public void Dispose()
    {
        ConfigFolderWatcher.Dispose();

        if (Directory.Exists(ConfigFolderPath))
        {
            Directory.Delete(ConfigFolderPath, true);
        }
    }
}
