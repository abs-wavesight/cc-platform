using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.LocalDevUtility.Commands.Configure;

public static class ConfigureCommand
{
    public static async Task<int> Configure(ConfigureOptions configureOptions, ILogger logger)
    {
        var readAppConfig = await ReadConfig();
        if (configureOptions.PrintOnly == true)
        {
            if (readAppConfig == null)
            {
                Console.WriteLine("Configuration was not found. Run \"configure\" command to initialize.");
            }
            else
            {
                Console.Write($"{nameof(readAppConfig.CommonCorePlatformRepositoryPath)}: {readAppConfig.CommonCorePlatformRepositoryPath}");
            }

            return 0;
        }

        Console.Write($"\"cc-platform\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCorePlatformRepositoryPath) ? $" ({readAppConfig.CommonCorePlatformRepositoryPath})" : "")}: ");
        var ccPlatformRepositoryLocalPath = Console.ReadLine() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccPlatformRepositoryLocalPath))
        {
            ccPlatformRepositoryLocalPath = readAppConfig?.CommonCorePlatformRepositoryPath;
        }

        Console.Write($"\"cc-drex\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCoreDrexRepositoryPath) ? $" ({readAppConfig.CommonCoreDrexRepositoryPath})" : "")}: ");
        var ccDrexRepositoryLocalPath = Console.ReadLine() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccDrexRepositoryLocalPath))
        {
            ccDrexRepositoryLocalPath = readAppConfig?.CommonCoreDrexRepositoryPath;
        }

        Console.Write($"Container Windows version, 2019 or 2022{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.ContainerWindowsVersion) ? $" ({readAppConfig.ContainerWindowsVersion})" : "")}: ");
        var containerWindowsVersion = Console.ReadLine() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(containerWindowsVersion))
        {
            containerWindowsVersion = readAppConfig?.ContainerWindowsVersion;
        }

        var appConfig = new AppConfig
        {
            CommonCorePlatformRepositoryPath = ccPlatformRepositoryLocalPath,
            CommonCoreDrexRepositoryPath = ccDrexRepositoryLocalPath,
            ContainerWindowsVersion = containerWindowsVersion
        };

        ValidateConfigAndThrow(appConfig);
        var fileName = await SaveConfig(appConfig);
        Console.WriteLine($"\nConfiguration saved ({fileName}):\n{JsonSerializer.Serialize(appConfig, new JsonSerializerOptions { WriteIndented = true })}");

        return 0;
    }

    public static void ValidateConfigAndThrow(AppConfig? appConfig)
    {
        var validationErrors = ValidateConfig(appConfig);
        if (validationErrors.Any())
        {
            throw new Exception("Configuration is invalid. Run \"cc-local configure\" to provide valid configuration values. Run \"cc-local configure -p\" to see your currently saved configuration values. Errors:\n" + string.Join("\n", validationErrors));
        }
    }

    public static List<string> ValidateConfig(AppConfig? appConfig)
    {
        var errors = new List<string>();

        if (appConfig == null)
        {
            errors.Add("Configuration not found");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(appConfig.CommonCorePlatformRepositoryPath) || !new DirectoryInfo(appConfig.CommonCorePlatformRepositoryPath).Exists)
        {
            errors.Add($"\"cc-platform\" repository path ({appConfig.CommonCorePlatformRepositoryPath}) could not be found");
        }

        if (string.IsNullOrWhiteSpace(appConfig.CommonCoreDrexRepositoryPath) || !new DirectoryInfo(appConfig.CommonCoreDrexRepositoryPath).Exists)
        {
            errors.Add($"\"cc-drex\" repository path ({appConfig.CommonCoreDrexRepositoryPath}) could not be found");
        }

        if (appConfig.ContainerWindowsVersion != "2019" && appConfig.ContainerWindowsVersion != "2022")
        {
            errors.Add($"Container Windows version ({appConfig.ContainerWindowsVersion}) is invalid (must be either \"2019\" or \"2022\")");
        }

        return errors;
    }

    public static async Task<AppConfig?> ReadConfig()
    {
        var configFileName = GetConfigFileName();
        if (!File.Exists(configFileName))
        {
            return null;
        }

        var appConfigJson = await File.ReadAllTextAsync(configFileName);
        return JsonSerializer.Deserialize<AppConfig>(appConfigJson) ?? null;
    }

    public static async Task<string> SaveConfig(AppConfig? appConfig)
    {
        var configFileName = GetConfigFileName();

        if (appConfig == null && File.Exists(configFileName))
        {
            File.Delete(configFileName);
            return "[Config File Deleted]";
        }

        var appConfigJson = JsonSerializer.Serialize(
            appConfig,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
        await File.WriteAllTextAsync(configFileName, appConfigJson);
        return configFileName;
    }

    private static string GetConfigFileName()
    {
        return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, Constants.ConfigFileName);
    }
}
