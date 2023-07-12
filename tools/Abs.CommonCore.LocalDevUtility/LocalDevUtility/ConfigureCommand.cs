using System.Reflection;
using System.Text.Json;
using Abs.CommonCore.LocalDevUtility.Models;

namespace Abs.CommonCore.LocalDevUtility;

public static class ConfigureCommand
{
    public static async Task<int> Configure(ConfigureOptions configureOptions)
    {
        if (configureOptions.PrintOnly == true)
        {
            var readAppConfig = await ReadConfig();
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

        Console.Write("\"cc-platform\" repository local path: ");
        var ccPlatformRepositoryLocalPath = Console.ReadLine() ?? "";

        Console.Write("\"cc-drex\" repository local path: ");
        var ccDrexRepositoryLocalPath = Console.ReadLine() ?? "";

        Console.Write("Container Windows version (2019 or 2022): ");
        var containerWindowsVersion = Console.ReadLine() ?? "";

        var appConfig = new AppConfig
        {
            CommonCorePlatformRepositoryPath = ccPlatformRepositoryLocalPath,
            CommonCoreDrexRepositoryPath = ccDrexRepositoryLocalPath,
            ContainerWindowsVersion = containerWindowsVersion
        };

        ValidateConfigAndThrow(appConfig);

        await SaveConfig(appConfig);

        Console.WriteLine("Configuration complete.");
        return 0;
    }

    public static void ValidateConfigAndThrow(AppConfig? appConfig)
    {
        var validationErrors = ValidateConfig(appConfig);
        if (validationErrors.Any())
        {
            throw new Exception("Configuration is invalid:\n" + string.Join("\n", validationErrors));
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

        var ccPlatformRepositoryDirectory = new DirectoryInfo(appConfig.CommonCorePlatformRepositoryPath);
        if (!ccPlatformRepositoryDirectory.Exists)
        {
            errors.Add($"\"cc-platform\" repository path ({appConfig.CommonCorePlatformRepositoryPath}) could not be found");
        }

        var ccDrexRepositoryDirectory = new DirectoryInfo(appConfig.CommonCoreDrexRepositoryPath);
        if (!ccDrexRepositoryDirectory.Exists)
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

    private static async Task SaveConfig(AppConfig appConfig)
    {
        var appConfigJson = JsonSerializer.Serialize(
            appConfig,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(GetConfigFileName(), appConfigJson);
    }

    private static string GetConfigFileName()
    {
        return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, Constants.ConfigFileName);
    }
}
