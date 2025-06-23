using System.Text.Json;
using System.Web;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Extensions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Extensions;

namespace Abs.CommonCore.Installer.Actions;
internal class PostInstallActions(ILogger logger, ICommandExecutionService commandExecutionService)
{
    private const string LocalRabbitUsername = "guest";
    private const string LocalRabbitPassword = "guest";
    private const string DrexSiteUsername = "drex";
    private const string VectorUsername = "vector";
    private const string DiscoSiteUsername = "disco";
    private const string SiemensSiteUsername = "siemens-adapter";
    private const string KdiSiteUsername = "kdi-adapter";
    private const string VMReportUsername = "vm-report-adapter";
    private const string MessageSchedulerUsername = "message-scheduler";

    internal async Task RunPostDrexInstallCommandAsync(
        Component component,
        string _,
        ComponentAction action,
        AccountType accountType,
        InstallationEnvironment installationEnvironment)
    {
        try
        {
            logger.LogInformation($"{component.Name}: Running Drex post install for '{action.Destination}'. Account {accountType}");
            logger.LogInformation(LocalRabbitLocation(installationEnvironment).ToString());
            logger.LogInformation(DrexSiteUsername);

            await UpdateRabbitCredentials(
                "DREX_SHARED_LOCAL_USERNAME",
                "DREX_SHARED_LOCAL_PASSWORD",
                accountType,
                action.Destination,
                DrexSiteUsername,
                action.Source,
                installationEnvironment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Drex account");
            throw;
        }
    }

    internal async Task RunPostRabbitMqInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        logger.LogInformation($"{component.Name}: Running RabbitMq post install for '{action.Source}'");

        var configText = await File.ReadAllTextAsync(action.Source);
        var password = RabbitConfigurer.GeneratePassword();

        // Replace the guest password with a new one
        var newText = configText
            .RequireReplace("\"password\": \"guest\",", $"\"password\": \"{password}\",");

        logger.LogInformation("Altering guest account");
        await File.WriteAllTextAsync(action.Source, newText);
    }

    internal async Task RunPostDrexCentralInstallCommandAsync(Component component, string rootLocation, ComponentAction action, InstallationEnvironment installationEnvironment)
    {
        await RunPostDrexInstallCommandAsync(component, rootLocation, action, AccountType.RemoteDrex, installationEnvironment);
    }

    internal async Task RunPostDiscoInstallCommandAsync(Component component, string rootLocation, ComponentAction action, InstallationEnvironment installationEnvironment)
    {
        logger.LogInformation($"{component.Name}: Running DISCO post install for '{action.Destination}'");
        await UpdateRabbitCredentials("DISCO_RABBIT_USERNAME", "DISCO_RABBIT_PASSWORD", AccountType.Disco, action.Destination, DiscoSiteUsername, action.Source, installationEnvironment);
    }

    internal async Task RunPostSiemensInstallCommandAsync(Component component, string rootLocation, ComponentAction action, InstallationEnvironment installationEnvironment)
    {
        logger.LogInformation($"{component.Name}: Running Siemens post install for '{action.Destination}'");
        await UpdateRabbitCredentials("SIEMENS_RABBIT_USERNAME", "SIEMENS_RABBIT_PASSWORD", AccountType.Siemens, action.Destination, SiemensSiteUsername, action.Source, installationEnvironment);
    }

    internal async Task RunPostKdiInstallCommandAsync(Component component, string rootLocation, ComponentAction action, InstallationEnvironment installationEnvironment)
    {
        logger.LogInformation($"{component.Name}: Running Kdi post install for '{action.Destination}'");
        await UpdateRabbitCredentials("KDI_RABBIT_USERNAME", "KDI_RABBIT_PASSWORD", AccountType.Kdi, action.Destination, KdiSiteUsername, action.Source, installationEnvironment);
    }

    internal async Task RunPostVectorInstallCommandAsync(
        Component component,
        string rootLocation,
        ComponentAction action,
        InstallationEnvironment installationEnvironment)
    {
        logger.LogInformation($"{component.Name}: Running Vector post install for '{action.Destination}'");
        var adminUser = await GetRabbitAdminUser(action.Source);

        var account = await RabbitConfigurer
            .ConfigureRabbitAsync(LocalRabbitLocation(installationEnvironment), adminUser.Name,
                                  adminUser.Password, VectorUsername, null,
                                  AccountType.Vector, true);

        var config = await File.ReadAllTextAsync(action.Destination);

        // Replace the vector account credentials
        var newText = config
                      .RequireReplace($"{LocalRabbitUsername}:{LocalRabbitPassword}", $"{account!.Username}:{HttpUtility.UrlEncode(account.Password)}");

        logger.LogInformation("Updating vector account");
        await File.WriteAllTextAsync(action.Destination, newText);
    }

    internal async Task RunPostVoyageManagerInstallCommandAsync(Component component, string rootLocation, ComponentAction action, InstallationEnvironment installationEnvironment)
    {
        logger.LogInformation($"{component.Name}: Running Voyage Report Manager post install for '{action.Destination}'");
        await UpdateRabbitCredentials("VOYAGE_MANAGER_RABBIT_USERNAME", "VOYAGE_MANAGER_RABBIT_PASSWORD", AccountType.VMReport, action.Destination, VMReportUsername, action.Source, installationEnvironment);
    }

    internal async Task RunPostMessageSchedulerInstallCommandAsync(Component component, string rootLocation, ComponentAction action, InstallationEnvironment installationEnvironment)
    {
        logger.LogInformation($"{component.Name}: Running Message Scheduler post install for '{action.Destination}'");
        await UpdateRabbitCredentials("MESSAGE_SCHEDULER_USERNAME", "MESSAGE_SCHEDULER_PASSWORD", AccountType.LocalDrex, action.Destination, MessageSchedulerUsername, action.Source, installationEnvironment);
    }

    internal async Task RunPostInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        logger.LogInformation($"{component.Name}: Running post install for '{action.Source}'");
        var parts = action.Source.Split(' ');
        await commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
    }

    private Uri LocalRabbitLocation(InstallationEnvironment installationEnvironment) => installationEnvironment switch
    {
        InstallationEnvironment.Central => new Uri("https://localhost:15671"),
        InstallationEnvironment.Site => new Uri("http://localhost:15672"),
        _ => throw new ArgumentException("Invalid installation environment")
    };

    private async Task UpdateRabbitCredentials(string usernameEnvVar,
        string passwordEnvVar,
        AccountType accountType,
        string envFilePath,
        string updatingUserName,
        string rabbitDefinitionFile,
        InstallationEnvironment installationEnvironment)
    {
        var adminUser = await GetRabbitAdminUser(rabbitDefinitionFile);

        var account = await RabbitConfigurer
                .ConfigureRabbitAsync(LocalRabbitLocation(installationEnvironment), adminUser.Name,
                                      adminUser.Password, updatingUserName, null,
                                      accountType, true);

        var envFile = await File.ReadAllTextAsync(envFilePath);
        var envLines = envFile.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        var userNameLineFound = false;
        var userPassLineFound = false;
        for (var i = 0; i < envLines.Count; i++)
        {
            if (envLines[i].Contains(usernameEnvVar))
            {
                logger.LogInformation($"Updating {usernameEnvVar} in env file");
                userNameLineFound = true;
                envLines[i] = $"{usernameEnvVar}={account!.Username}";
            }

            if (envLines[i].Contains(passwordEnvVar))
            {
                logger.LogInformation($"Updating {passwordEnvVar} in env file");
                userPassLineFound = true;
                envLines[i] = $"{passwordEnvVar}={account!.Password}";
            }

            if (userNameLineFound && userPassLineFound)
            {
                break;
            }
        }

        if (!userNameLineFound)
        {
            logger.LogInformation($"Adding {usernameEnvVar} to env file");
            envLines.Add($"{usernameEnvVar}={account!.Username}");
        }

        if (!userPassLineFound)
        {
            logger.LogInformation($"Adding {passwordEnvVar} to env file");
            envLines.Add($"{passwordEnvVar}={account!.Password}");
        }

        var newText = envLines.StringJoin(Environment.NewLine);

        logger.LogInformation("Updating env file {envFilePath}", envFilePath);
        await File.WriteAllTextAsync(envFilePath, newText);
    }

    private static async Task<RabbitMqUserModel> GetRabbitAdminUser(string rabbitDefinitionFile)
    {
        var configText = await File.ReadAllTextAsync(rabbitDefinitionFile);
        var definition = JsonSerializer.Deserialize<RabbitDefinitionModel>(configText);

        var adminUser = definition!.Users.FirstOrDefault(x => x.Name == LocalRabbitUsername)
            ?? throw new Exception($"Unable to find admin user '{LocalRabbitUsername}' in rabbit definition file");
        return adminUser;
    }
}
