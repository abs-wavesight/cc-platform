using System.Reflection;
using System.Text.Json;
using Abs.CommonCore.LocalDevUtility.Extensions;
using Abs.CommonCore.LocalDevUtility.Helpers;
using Abs.CommonCore.Platform;
using Abs.CommonCore.Platform.Certificates;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.LocalDevUtility.Commands.Configure;

public static class ConfigureCommand
{
    public static async Task<int> Configure(ConfigureOptions configureOptions, ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var readAppConfig = await ReadConfig();
        if (configureOptions.PrintOnly == true)
        {
            logger.LogInformation(readAppConfig == null
                ? "Configuration was not found. Run \"configure\" command to initialize."
                : $"{JsonSerializer.Serialize(readAppConfig, new JsonSerializerOptions { WriteIndented = true })}");
            return 0;
        }

        Console.Write($"\"cc-platform\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCorePlatformRepositoryPath) ? $" ({readAppConfig.CommonCorePlatformRepositoryPath})" : "")}: ");
        var ccPlatformRepositoryLocalPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccPlatformRepositoryLocalPath))
        {
            ccPlatformRepositoryLocalPath = readAppConfig?.CommonCorePlatformRepositoryPath;
        }

        Console.Write($"\"cc-drex\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCoreDrexRepositoryPath) ? $" ({readAppConfig.CommonCoreDrexRepositoryPath})" : "")}: ");
        var ccDrexRepositoryLocalPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccDrexRepositoryLocalPath))
        {
            ccDrexRepositoryLocalPath = readAppConfig?.CommonCoreDrexRepositoryPath;
        }

        Console.Write($"\"cc-disco\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCoreDiscoRepositoryPath) ? $" ({readAppConfig.CommonCoreDiscoRepositoryPath})" : "")}: ");
        var ccDiscoRepositoryLocalPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccDiscoRepositoryLocalPath))
        {
            ccDiscoRepositoryLocalPath = readAppConfig?.CommonCoreDiscoRepositoryPath;
        }

        Console.Write($"\"cc-adapters-siemens\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCoreSiemensAdapterRepositoryPath) ? $" ({readAppConfig.CommonCoreSiemensAdapterRepositoryPath})" : "")}: ");
        var ccSiemensAdapterRepositoryLocalPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccSiemensAdapterRepositoryLocalPath))
        {
            ccSiemensAdapterRepositoryLocalPath = readAppConfig?.CommonCoreSiemensAdapterRepositoryPath;
        }

        Console.Write($"\"cc-adapters-kdi\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCoreKdiAdapterRepositoryPath) ? $" ({readAppConfig.CommonCoreKdiAdapterRepositoryPath})" : "")}: ");
        var ccKdiAdapterRepositoryLocalPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccKdiAdapterRepositoryLocalPath))
        {
            ccKdiAdapterRepositoryLocalPath = readAppConfig?.CommonCoreKdiAdapterRepositoryPath;
        }

        Console.Write($"\"cc-voyage-manager-adapter\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.VoyageManagerRepositoryPath) ? $" ({readAppConfig.VoyageManagerRepositoryPath})" : "")}: ");
        var voyageManagerRepositoryPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(voyageManagerRepositoryPath))
        {
            voyageManagerRepositoryPath = readAppConfig?.VoyageManagerRepositoryPath;
        }

        Console.Write($"\"cc-scheduler\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCoreSchedulerRepositoryPath) ? $" ({readAppConfig.CommonCoreSchedulerRepositoryPath})" : "")}: ");
        var ccSchedulerRepositoryPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccSchedulerRepositoryPath))
        {
            ccSchedulerRepositoryPath = readAppConfig?.CommonCoreSchedulerRepositoryPath;
        }

        Console.Write($"\"cc-drex-notification-adapter\" repository local path{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCoreSchedulerRepositoryPath) ? $" ({readAppConfig.CommonCoreDrexNotificationAdapterRepositoryPath})" : "")}: ");
        var ccDrexNotificationAdapterRepositoryPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccDrexNotificationAdapterRepositoryPath))
        {
            ccDrexNotificationAdapterRepositoryPath = readAppConfig?.CommonCoreDrexNotificationAdapterRepositoryPath;
        }

        Console.Write($"Container Windows version, 2019 or 2022{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.ContainerWindowsVersion) ? $" ({readAppConfig.ContainerWindowsVersion})" : "")}: ");
        var containerWindowsVersion = Console.ReadLine() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(containerWindowsVersion))
        {
            containerWindowsVersion = readAppConfig?.ContainerWindowsVersion;
        }

        Console.Write($"Local path to use for storage of generated TLS certificates used by containers{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CertificatePath) ? $" ({readAppConfig.CertificatePath})" : "")}: ");
        var certificatePath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            certificatePath = readAppConfig?.CertificatePath;
        }

        Console.Write($"Local path to use for storage of generated SSH keys used by containers{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.SshKeysPath) ? $" ({readAppConfig.SshKeysPath})" : "")}: ");
        var sshKeysPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sshKeysPath))
        {
            sshKeysPath = readAppConfig?.SshKeysPath;
        }

        Console.Write($"Local path to use for SFTP root -- will be created if it does not exist{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.SftpRootPath) ? $" ({readAppConfig.SftpRootPath})" : "")}: ");
        var sftpRootPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sftpRootPath))
        {
            sftpRootPath = readAppConfig?.SftpRootPath;
        }

        var currentValue = readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.FdzRootPath) ? $" ({readAppConfig.FdzRootPath})" : "";
        Console.Write($"Local path to use for File Drop Zone root -- will be created if it does not exist{currentValue}: ");
        var fdzRootPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fdzRootPath))
        {
            fdzRootPath = readAppConfig?.FdzRootPath;
        }

        Console.Write($"Local path to use for log files created by Drex Notification Adapter{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.CommonCoreCliensLogs) ? $" ({readAppConfig.CommonCoreCliensLogs})" : "")}: ");
        var ccLogsPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ccLogsPath))
        {
            ccLogsPath = readAppConfig?.CommonCoreCliensLogs;
        }

        Console.Write("Generate and install certificates now -- this only needs to be done once ever (y/n)? (n): ");
        var generateCertificatesNow = false;
        var generateCertificatesNowInput = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(generateCertificatesNowInput))
        {
            var validPositiveValues = new List<string> { "y", "yes", "true" };
            generateCertificatesNow = validPositiveValues.Contains(generateCertificatesNowInput.ToLowerInvariant());
        }

        Console.Write("Generate SSH keys -- this only needs to be done once ever (y/n)? (n): ");
        var generateSshKeysNow = false;
        var generateSshKeysNowInput = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(generateSshKeysNowInput))
        {
            var validPositiveValues = new List<string> { "y", "yes", "true" };
            generateSshKeysNow = validPositiveValues.Contains(generateSshKeysNowInput.ToLowerInvariant());
        }

        var appConfig = new AppConfig
        {
            CommonCoreSchedulerRepositoryPath = ccSchedulerRepositoryPath,
            VoyageManagerRepositoryPath = voyageManagerRepositoryPath,
            CommonCorePlatformRepositoryPath = ccPlatformRepositoryLocalPath,
            CommonCoreDrexRepositoryPath = ccDrexRepositoryLocalPath,
            CommonCoreDiscoRepositoryPath = ccDiscoRepositoryLocalPath,
            CommonCoreSiemensAdapterRepositoryPath = ccSiemensAdapterRepositoryLocalPath,
            CommonCoreKdiAdapterRepositoryPath = ccKdiAdapterRepositoryLocalPath,
            CommonCoreDrexNotificationAdapterRepositoryPath = ccDrexNotificationAdapterRepositoryPath,
            ContainerWindowsVersion = containerWindowsVersion,
            CertificatePath = certificatePath,
            SshKeysPath = sshKeysPath,
            SftpRootPath = sftpRootPath,
            FdzRootPath = fdzRootPath, 
            CommonCoreCliensLogs = ccLogsPath
        };

        CreateDirectories(appConfig);
        ValidateConfigAndThrow(appConfig);
        SetEnvironmentVariables(appConfig);

        var fileName = await SaveConfig(appConfig);
        logger.LogInformation($"\nConfiguration saved ({fileName}):\n{JsonSerializer.Serialize(appConfig, new JsonSerializerOptions { WriteIndented = true })}");

        if (generateCertificatesNow)
        {
            using (CliStep.Start("Generating TLS certificates"))
            {
                // Ensure sub-directories exist
                Directory.CreateDirectory(Path.Combine(appConfig.CertificatePath!, Constants.CertificateSubDirectories.LocalKeys));
                Directory.CreateDirectory(Path.Combine(appConfig.CertificatePath!, Constants.CertificateSubDirectories.LocalCerts));
                Directory.CreateDirectory(Path.Combine(appConfig.CertificatePath!, Constants.CertificateSubDirectories.RemoteKeys));
                Directory.CreateDirectory(Path.Combine(appConfig.CertificatePath!, Constants.CertificateSubDirectories.RemoteCerts));

                var fullContainerName = $"{Constants.ContainerRepository}/openssl:windows-{appConfig.ContainerWindowsVersion}";
                var dockerCommand = $"docker pull {fullContainerName};";
                dockerCommand += " docker run";
                dockerCommand += $" --mount \"type=bind,source={appConfig.CommonCorePlatformRepositoryPath}/config/openssl,target=C:/config\" ";
                dockerCommand += GetMountParameterForCertDirectory(appConfig, Constants.CertificateSubDirectories.LocalKeys);
                dockerCommand += GetMountParameterForCertDirectory(appConfig, Constants.CertificateSubDirectories.LocalCerts);
                dockerCommand += GetMountParameterForCertDirectory(appConfig, Constants.CertificateSubDirectories.RemoteKeys);
                dockerCommand += GetMountParameterForCertDirectory(appConfig, Constants.CertificateSubDirectories.RemoteCerts);
                dockerCommand += $" {fullContainerName} pwsh \"C:/config/generate-certs.ps1\" ";
                logger.LogInformation($"Running docker command: {dockerCommand}");
                powerShellAdapter.RunPowerShellCommand(dockerCommand);
            }

            using (CliStep.Start("Installing TLS certificates", true))
            {
                const string rabbitMqCertName = "ca.pem";
                CertificateImporter.ImportCertificate($"{appConfig.CertificatePath}/{Constants.CertificateSubDirectories.LocalCerts}/{rabbitMqCertName}", null, logger);
                CertificateImporter.ImportCertificate($"{appConfig.CertificatePath}/{Constants.CertificateSubDirectories.RemoteCerts}/{rabbitMqCertName}", null, logger);
            }
        }

        if (generateSshKeysNow)
        {
            using (CliStep.Start("Generating SSH keys"))
            {
                var command = $"dotnet run --project {appConfig.CommonCorePlatformRepositoryPath}/services/SftpService/SftpService.csproj -- gen-key --path {appConfig.SshKeysPath}";
                logger.LogInformation($"Running command: {command}");
                powerShellAdapter.RunPowerShellCommand(command);
            }
        }

        return 0;
    }

    private static string GetMountParameterForCertDirectory(AppConfig appConfig, string certDirectoryName)
    {
        return $" --mount \"type=bind,source={appConfig.CertificatePath}/{certDirectoryName},target=C:/{certDirectoryName}\"";
    }

    public static void ValidateConfigAndThrow(AppConfig appConfig)
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

        if (string.IsNullOrWhiteSpace(appConfig.CommonCoreDiscoRepositoryPath) || !new DirectoryInfo(appConfig.CommonCoreDiscoRepositoryPath).Exists)
        {
            errors.Add($"\"cc-disco\" repository path ({appConfig.CommonCoreDiscoRepositoryPath}) could not be found");
        }

        if (string.IsNullOrWhiteSpace(appConfig.CommonCoreSiemensAdapterRepositoryPath) || !new DirectoryInfo(appConfig.CommonCoreSiemensAdapterRepositoryPath).Exists)
        {
            errors.Add($"\"cc-adapters-siemens\" repository path ({appConfig.CommonCoreSiemensAdapterRepositoryPath}) could not be found");
        }

        if (string.IsNullOrWhiteSpace(appConfig.CommonCoreKdiAdapterRepositoryPath) || !new DirectoryInfo(appConfig.CommonCoreKdiAdapterRepositoryPath).Exists)
        {
            errors.Add($"\"cc-adapters-kdi\" repository path ({appConfig.CommonCoreKdiAdapterRepositoryPath}) could not be found");
        }

        if (string.IsNullOrWhiteSpace(appConfig.VoyageManagerRepositoryPath) || !new DirectoryInfo(appConfig.VoyageManagerRepositoryPath).Exists)
        {
            errors.Add($"\"cc-voyage-manager-adapter\" repository path ({appConfig.VoyageManagerRepositoryPath}) could not be found");
        }

        if (string.IsNullOrWhiteSpace(appConfig.CommonCoreSchedulerRepositoryPath) || !new DirectoryInfo(appConfig.CommonCoreSchedulerRepositoryPath).Exists)
        {
            errors.Add($"\"cc-scheduler\" repository path ({appConfig.CommonCoreSchedulerRepositoryPath}) could not be found");
        }

        if (string.IsNullOrWhiteSpace(appConfig.CommonCoreDrexNotificationAdapterRepositoryPath) || !new DirectoryInfo(appConfig.CommonCoreDrexNotificationAdapterRepositoryPath).Exists)
        {
            errors.Add($"\"cc-drex-notification-adapter\" repository path ({appConfig.CommonCoreDrexNotificationAdapterRepositoryPath}) could not be found");
        }

        if (appConfig.ContainerWindowsVersion is not "2019" and not "2022")
        {
            errors.Add($"Container Windows version ({appConfig.ContainerWindowsVersion}) is invalid (must be either \"2019\" or \"2022\")");
        }

        if (string.IsNullOrWhiteSpace(appConfig.CertificatePath) || !new DirectoryInfo(appConfig.CertificatePath).Exists)
        {
            errors.Add($"Certificate path ({appConfig.CertificatePath}) could not be found");
        }

        if (string.IsNullOrWhiteSpace(appConfig.SshKeysPath) || !new DirectoryInfo(appConfig.SshKeysPath).Exists)
        {
            errors.Add($"SSH keys path ({appConfig.SshKeysPath}) could not be found");
        }

        if (string.IsNullOrWhiteSpace(appConfig.SftpRootPath) || !new DirectoryInfo(appConfig.SftpRootPath).Exists)
        {
            errors.Add($"SFTP root path ({appConfig.SftpRootPath}) is required");
        }

        if (string.IsNullOrWhiteSpace(appConfig.FdzRootPath) || !new DirectoryInfo(appConfig.FdzRootPath).Exists)
        {
            errors.Add($"FDZ root path ({appConfig.FdzRootPath}) is required");
        }

        if (string.IsNullOrWhiteSpace(appConfig.CommonCoreCliensLogs) || !new DirectoryInfo(appConfig.CommonCoreCliensLogs).Exists)
        {
            errors.Add($"CommonCore applications logs path ({appConfig.CommonCoreCliensLogs}) is required");
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

    private static void SetEnvironmentVariables(AppConfig appConfig)
    {
        Environment.SetEnvironmentVariable(PlatformConstants.FDZ_Path, appConfig.FdzRootPath, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable(PlatformConstants.SFTP_Path, appConfig.SftpRootPath, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable(PlatformConstants.SSH_Keys_Path, appConfig.SshKeysPath, EnvironmentVariableTarget.User);
    }

    private static void CreateDirectories(AppConfig appConfig)
    {
        if (!Directory.Exists(appConfig.CertificatePath))
        {
            using (CliStep.Start("Creating Certificate root directory"))
            {
                Directory.CreateDirectory(appConfig.CertificatePath!);
            }
        }

        if (!Directory.Exists(appConfig.SftpRootPath))
        {
            using (CliStep.Start("Creating SFTP root directory"))
            {
                Directory.CreateDirectory(appConfig.SftpRootPath!);
            }
        }

        if (!Directory.Exists(appConfig.FdzRootPath))
        {
            using (CliStep.Start("Creating FDZ root directory"))
            {
                Directory.CreateDirectory(appConfig.FdzRootPath!);
            }
        }

        if (!Directory.Exists(appConfig.SshKeysPath))
        {
            using (CliStep.Start("Creating SSH keys root directory"))
            {
                Directory.CreateDirectory(appConfig.SshKeysPath!);
            }
        }
    }
}
