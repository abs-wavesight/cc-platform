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

        Console.Write($"Local path to use for SFTP (OpenSSH) root -- will be created if it does not exist{(readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.SftpRootPath) ? $" ({readAppConfig.SftpRootPath})" : "")}: ");
        var sftpRootPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sftpRootPath))
        {
            sftpRootPath = readAppConfig?.SftpRootPath;
        }

        var currentValue = (readAppConfig != null && !string.IsNullOrEmpty(readAppConfig.FdzRootPath) ? $" ({readAppConfig.FdzRootPath})" : "");
        Console.Write($"Local path to use for File Drop Zone root -- will be created if it does not exist{currentValue}: ");
        var fdzRootPath = Console.ReadLine()?.TrimTrailingSlash().ToForwardSlashes() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fdzRootPath))
        {
            fdzRootPath = readAppConfig?.FdzRootPath;
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
            CommonCorePlatformRepositoryPath = ccPlatformRepositoryLocalPath,
            CommonCoreDrexRepositoryPath = ccDrexRepositoryLocalPath,
            ContainerWindowsVersion = containerWindowsVersion,
            CertificatePath = certificatePath,
            SshKeysPath = sshKeysPath,
            SftpRootPath = sftpRootPath,
            FdzRootPath = fdzRootPath,
        };

        ValidateConfigAndThrow(appConfig);
        SetEnvironmentVariables(appConfig);
        CreateDirectories(appConfig);

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
                const string rabbitMqCertName = "rabbitmq.cer";
                CertificateImporter.ImportCertificate($"{appConfig.CertificatePath}/{Constants.CertificateSubDirectories.LocalCerts}/{rabbitMqCertName}", null, logger);
                CertificateImporter.ImportCertificate($"{appConfig.CertificatePath}/{Constants.CertificateSubDirectories.RemoteCerts}/{rabbitMqCertName}", null, logger);
            }
        }

        if (generateSshKeysNow)
        {
            InstallOpenSsh(powerShellAdapter);

            using (CliStep.Start("Generating SSH keys"))
            {
                Directory.CreateDirectory(appConfig.SshKeysPath!);
                
                // OpenSSH client must be enabled
                var command = $"{appConfig.CommonCorePlatformRepositoryPath}/config/openssh/create-ssh-keys-and-fingerprint.ps1 {appConfig.SshKeysPath}";
                logger.LogInformation($"Running command: {command}");
                powerShellAdapter.RunPowerShellCommand(command);
            }
        }

        return 0;
    }

    private static void InstallOpenSsh(IPowerShellAdapter powerShellAdapter)
    {
        using (CliStep.Start("Installing SSH client."))
        {
            // Details: https://learn.microsoft.com/en-us/windows-server/administration/openssh/openssh_install_firstuse?tabs=powershell
            const string getStatusCommand = "Get-WindowsCapability -Online | Where-Object Name -like 'OpenSSH.Client*'";
            const string installedStatus = "Installed";
            var results = powerShellAdapter.RunPowerShellCommand(getStatusCommand);
            var isOpenSshClientInstalled = results.Any(r => r.Contains(installedStatus));

            if (!isOpenSshClientInstalled)
            {
                const string installCommand = "Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0";
                powerShellAdapter.RunPowerShellCommand(installCommand);
            }
        }
    }

    private static void SetEnvironmentVariables(AppConfig appConfig)
    {
        Environment.SetEnvironmentVariable(PlatformConstants.SFTP_Path, appConfig.SftpRootPath!);
        Environment.SetEnvironmentVariable(PlatformConstants.FDZ_Path, appConfig.FdzRootPath!);
        Environment.SetEnvironmentVariable(PlatformConstants.SSH_Keys_Path, appConfig.SshKeysPath!);
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

        if (appConfig.ContainerWindowsVersion != "2019" && appConfig.ContainerWindowsVersion != "2022")
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

        if (string.IsNullOrWhiteSpace(appConfig.SftpRootPath))
        {
            errors.Add($"SFTP root path ({appConfig.SftpRootPath}) is required");
        }

        if (string.IsNullOrWhiteSpace(appConfig.FdzRootPath))
        {
            errors.Add($"FDZ root path ({appConfig.FdzRootPath}) is required");
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
        Environment.SetEnvironmentVariable(PlatformConstants.FDZ_Path, appConfig.FdzRootPath, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable(PlatformConstants.SFTP_Path, appConfig.SftpRootPath, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable(PlatformConstants.SSH_Keys_Path, appConfig.SshKeysPath, EnvironmentVariableTarget.Machine);
    }

    private static void CreateDirectories(AppConfig appConfig)
    {
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
