using System.Management;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Contracts.Proto;
using Abs.CommonCore.Contracts.Proto.Installer;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Extensions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Polly;
using JsonParser = Abs.CommonCore.Installer.Services.JsonParser;

#pragma warning disable CA1416
namespace Abs.CommonCore.Installer.Actions;

public class ComponentInstaller : ActionBase
{
    private const string LocalRabbitUsername = "guest";
    private const string LocalRabbitPassword = "guest";
    private const string DrexSiteUsername = "drex";
    private const string VectorUsername = "vector";
    private const string DockerServiceName = "dockerd";
    private const string DiscoSiteUsername = "disco";
    private const string SiemensSiteUsername = "siemens-adapter";
    private const string KdiSiteUsername = "kdi-adapter";
    private const string VMReportUsername = "vm-report-adapter";
    private const string MessageSchedulerUsername = "message-scheduler";

    private const int DefaultMaxChunkSize = 1 * 1024 * 1024 * 1024; // 1GB
    private const string ReleaseZipName = "Release.zip";
    private const string AdditionalFilesName = "AdditionalFiles";

    private const string Win32OptionalFeatures = "Win32_OptionalFeature";
    private const string Win32ContainerFeature = "Containers";
    private const string Win32InstalledStatus = "InstallState";
    private const string Win32PropertyName = "Name";

    // https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-optionalfeature
    private const string Win32FeatureEnabled = "1"; // 1 is enabled, 2 is disabled

    private readonly ILoggerFactory _loggerFactory;
    private readonly ICommandExecutionService _commandExecutionService;
    private readonly IServiceManager _serviceManager;
    private readonly ILogger _logger;

    private readonly InstallerComponentInstallerConfig? _installerConfig;
    private readonly InstallerComponentRegistryConfig _registryConfig;
    private readonly Dictionary<string, string> _allParameters;
    private const string _imageStorage = "ghcr.io/abs-wavesight/";

    public bool WaitForDockerContainersHealthy { get; set; } = true;

    private Uri LocalRabbitLocation => _allParameters.GetInstallationEnvironment() switch
    {
        InstallationEnvironment.Central => new Uri("https://localhost:15671"),
        InstallationEnvironment.Site => new Uri("http://localhost:15672"),
        _ => throw new ArgumentException("Invalid installation environment")
    };

    public ComponentInstaller(
        ILoggerFactory loggerFactory,
        ICommandExecutionService commandExecutionService,
        IServiceManager serviceManager,
        FileInfo registryConfig,
        FileInfo? installerConfig,
        Dictionary<string, string> parameters,
        bool promptForMissingParameters)
    {
        _loggerFactory = loggerFactory;
        _commandExecutionService = commandExecutionService;
        _serviceManager = serviceManager;
        _logger = loggerFactory.CreateLogger<ComponentInstaller>();

        _installerConfig = installerConfig != null
            ? ConfigParser.LoadConfig<InstallerComponentInstallerConfig>(installerConfig.FullName)
            : null;

        var mergedParameters = _installerConfig?.Parameters ?? new Dictionary<string, string>();
        mergedParameters.MergeParameters(parameters);

        if (promptForMissingParameters)
        {
            ReadMissingParameters(mergedParameters);
        }

        _allParameters = mergedParameters;
        _registryConfig = JsonParser.Instance.Load<InstallerComponentRegistryConfig>(registryConfig.FullName,
            (c, t) => t.ReplaceConfigParameters(mergedParameters));
    }

    public async Task ExecuteAsync(string[]? specificComponents = null)
    {
        if (string.IsNullOrWhiteSpace(_registryConfig.Location))
        {
            throw new Exception("Location must be specified");
        }

        _logger.LogInformation("Starting installer");
        var shouldContinue = await EnableContainersAsync();

        if (!shouldContinue)
        {
            return;
        }

        var dockerPath = DockerPath.GetDockerPath();
        var readmeLines = await PrintReadmeFileAsync();
        await ExpandReleaseZipFile();
        var cleaningScriptPath = _commandExecutionService.GetCleaningScriptPath(_registryConfig.Location);

        var imageListTxtPath = Path.Combine(cleaningScriptPath, "image_list.txt");
        if (File.Exists(imageListTxtPath))
        {
            File.Delete(imageListTxtPath);
        }

        var dockerRun = true;

        try
        {
            await _commandExecutionService.ExecuteCommandAsync(dockerPath, "ps", "");
        }
        catch
        {
            dockerRun = false;
            _logger.LogInformation("Docker is not running.");
        }

        var widowsVersionSpecified = false;
        string[][] installingVesrion_Component = null;
        var windowsVersion = "";
        try
        {
            windowsVersion = _allParameters.GetWindowsVersion();
            widowsVersionSpecified = true;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Unable to get windows version");
        }

        if (!widowsVersionSpecified)
        {
            windowsVersion = readmeLines[0].Split(":")[1].Trim();
            installingVesrion_Component = readmeLines.Skip(3).Select(c => c.Split(":")[1].Trim().Split("_")).ToArray();
            var resultContainers = installingVesrion_Component.Select(x => $"{_imageStorage}{x[1]}:windows-{windowsVersion}-{x[0]}").ToArray();
            await File.WriteAllLinesAsync(imageListTxtPath, resultContainers);

            if (dockerRun)
            {
                await _commandExecutionService.ExecuteCommandAsync("powershell", $"-File cleanup.ps1 -DockerPath {dockerPath}", cleaningScriptPath);
            }
        }

        var components = await DetermineComponents(specificComponents, installingVesrion_Component, dockerRun);
        VerifySourcesPresent(components);

        var orderedComponenets = components
            .OrderByDescending(x => x.Name == "Installer")
            .ThenByDescending(x => x.Name == "Docker")
            .ThenByDescending(x => x.Name is "Certificates-Central" or "Certificates-Site")
            .ThenByDescending(x => x.Name == "OpenSsl")
            .ThenByDescending(x => x.Name == "RabbitMq")
            .ThenByDescending(x => x.Name == "Vector")
            .ThenByDescending(x => x.Name == "Sftp-Service")
            .ThenByDescending(x => x.Name is "Drex-Message" or "Drex-Central-Message")
            .ThenByDescending(x => x.Name is "Drex-File" or "Voyage-Manager-Report-Adapter" or "Message-Scheduler")
            .ThenByDescending(x => x.Name == "Disco")
            .ThenByDescending(x => x.Name is "Siemens" or "Kdi")
            .ThenByDescending(x => x.Name == "Observability-Service")
            .ToArray();

        foreach (var component in orderedComponenets)
        {
            var orderedActions = component.Actions
            .OrderByDescending(x => x.Action == ComponentActionAction.RequestCertificates)
            .ThenByDescending(x => x.Action == ComponentActionAction.Copy)
            .ThenByDescending(x => x.Action == ComponentActionAction.ValidateJson)
            .ThenByDescending(x => x.Action == ComponentActionAction.ReplaceParameters)
            .ThenByDescending(x => x.Action == ComponentActionAction.ExecuteImmediate)
            .ThenByDescending(x => x.Action == ComponentActionAction.Install)
            .ThenByDescending(x => x.Action == ComponentActionAction.Execute)
            .ThenByDescending(x => x.Action == ComponentActionAction.UpdatePath)
            .ThenByDescending(x =>
                x.Action is ComponentActionAction.Chunk or
                    ComponentActionAction.Unchunk or
                    ComponentActionAction.Compress or
                    ComponentActionAction.Uncompress)
            .ThenByDescending(x => x.Action == ComponentActionAction.PostRabbitMqInstall)
            .ThenByDescending(x =>
                x.Action is ComponentActionAction.PostDrexInstall or
                    ComponentActionAction.PostVectorInstall or
                    ComponentActionAction.PostDiscoInstall or
                    ComponentActionAction.PostSiemensInstall or
                    ComponentActionAction.PostKdiInstall or
                    ComponentActionAction.PostVMReportInstall or
                    ComponentActionAction.PostDrexCentralInstall or
                    ComponentActionAction.PostMessageSchedulerInstall)
            .ThenByDescending(x => x.Action == ComponentActionAction.RunDockerCompose)
            //.ThenByDescending(x => x.Action == ComponentActionAction.PostInstall)
            //.ThenByDescending(x => x.Action == ComponentActionAction.SystemRestore)
            .ToArray();

            foreach (var action in orderedActions)
            {
                await ProcessExecuteActionAsync(component, Path.Combine(_registryConfig.Location, component.Name), action);
            }
        }

        if (!widowsVersionSpecified)
        {
            await _commandExecutionService.ExecuteCommandAsync("powershell", $"-File cleanup.ps1 -DockerPath {dockerPath}", cleaningScriptPath);
        }

        _logger.LogInformation("Installer complete");
    }

    private async Task ProcessExecuteActionAsync(Component component, string rootLocation, ComponentAction action)
    {
        try
        {
            await (action.Action switch
            {
                ComponentActionAction.Execute => RunExecuteCommandAsync(component, rootLocation, action),
                ComponentActionAction.ExecuteImmediate => RunExecuteCommandAsync(component, rootLocation, action),
                ComponentActionAction.Copy => RunCopyCommandAsync(component, rootLocation, action),
                ComponentActionAction.Install => RunInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.UpdatePath => RunUpdatePathCommandAsync(component, rootLocation, action),
                ComponentActionAction.ReplaceParameters => RunReplaceParametersCommandAsync(component, rootLocation, action),
                ComponentActionAction.Chunk => RunChunkCommandAsync(component, rootLocation, action),
                ComponentActionAction.Unchunk => RunUnchunkCommandAsync(component, rootLocation, action),
                ComponentActionAction.Compress => RunCompressCommandAsync(component, rootLocation, action),
                ComponentActionAction.Uncompress => RunUncompressCommandAsync(component, rootLocation, action),
                ComponentActionAction.RunDockerCompose => RunDockerComposeCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostDrexInstall => RunPostDrexInstallCommandAsync(component, rootLocation, action, Models.AccountType.LocalDrex),
                ComponentActionAction.PostVectorInstall => RunPostVectorInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostRabbitMqInstall => RunPostRabbitMqInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostVMReportInstall => RunPostVoyageManagerInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostMessageSchedulerInstall => RunPostMessageSchedulerInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostInstall => RunPostInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostDiscoInstall => RunPostDiscoInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostSiemensInstall => RunPostSiemensInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostKdiInstall => RunPostKdiInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.SystemRestore => RunSystemRestoreCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostDrexCentralInstall => RunPostDrexCentralInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.ValidateJson => RunValidateJsonCommandAsync(component, rootLocation, action),
                ComponentActionAction.RequestCertificates => RequestCertificatesCommandAsync(component, rootLocation, action),
                _ => throw new Exception($"Unknown action command: {action.Action}")
            });
        }
        catch (Exception ex)
        {
            var message = $"Unable to process install action. {JsonSerializer.Serialize(action)}";
            throw new Exception(message, ex);
        }
    }

    public async Task RunSystemRestoreCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running system restore for '{action.Source}'");
        await _serviceManager.StartServiceAsync(DockerServiceName);

        await StopAllContainersAsync();
        await DockerSystemPruneAsync();

        await ExecuteDockerComposeAsync(rootLocation, action);
    }

    private async Task<Component[]> DetermineComponents(string[]? specificComponents, string[][] installingVesrion_Component, bool dockerRun)
    {
        try
        {
            var installLocation = new DirectoryInfo(_registryConfig.Location);

            if (specificComponents?.Length > 0)
            {
                return specificComponents
                    .Select(x => _registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
            }

            if (_installerConfig?.Components.Count > 0)
            {
                var defaultComponentsToInstall = _installerConfig.Components
                    .Select(x => _registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
                var command = DockerPath.GetDockerPath();
                var currentContainers = new List<DockerContainerInfoModel>();
                if (dockerRun)
                {
                    var rawDockerPsResponse = _commandExecutionService.ExecuteCommandWithResult(command, "ps", "");
                    _logger.LogInformation("Parsing raw ps command response:");
                    foreach (var line in rawDockerPsResponse)
                    {
                        _logger.LogInformation("    {line}", line);
                    }

                    currentContainers = DockerService.ParceDockerPsCommand(rawDockerPsResponse);
                }

                _logger.LogInformation("Determining components to install");

                if (!dockerRun
                    || (defaultComponentsToInstall.Any(c => c.Name == "RabbitMqNano") && NeedUpdateComponent("RabbitMqNano", installingVesrion_Component, currentContainers))
                    || (defaultComponentsToInstall.Any(c => c.Name == "RabbitMq") && NeedUpdateComponent("RabbitMq", installingVesrion_Component, currentContainers)))
                {
                    _logger.LogInformation("Full installation is required");
                    if (dockerRun)
                    {
                        _logger.LogInformation("Stopping docker");
                        await _commandExecutionService.ExecuteCommandAsync(command, "network prune -f", "");

                        await _serviceManager.StopServiceAsync("dockerd");
                        await _serviceManager.StopServiceAsync("docker");
                    }

                    return defaultComponentsToInstall;
                }
                else
                {
                    _logger.LogInformation("Selecting components to update.");
                    return SelectUpdatedComponents(installingVesrion_Component, defaultComponentsToInstall, currentContainers).ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to determine components to use", ex);
        }

        throw new Exception("No components found to download");
    }

    private List<Component> SelectUpdatedComponents(string[][] installingVesrion_Component, Component[] defaultComponentsToInstall, List<DockerContainerInfoModel> currentContainers)
    {
        var componentsToInstall = new List<Component>();
        foreach (var component in defaultComponentsToInstall)
        {
            switch (component.Name)
            {
                case "Docker":
                case "RabbitMq":
                case "RabbitMqNano":
                case "Certificates-Central":
                    break;
                case "Installer":
                    componentsToInstall.Add(component);
                    break;
                default:
                    if (NeedUpdateComponent(component.Name, installingVesrion_Component, currentContainers))
                    {
                        componentsToInstall.Add(component);
                    }

                    break;
            }
        }

        _logger.LogInformation("Components to install: {components}", componentsToInstall.Select(x => x.Name).StringJoin(Environment.NewLine));
        return componentsToInstall;
    }

    private bool NeedUpdateComponent(string componentName, string[][] installingVesrion_Component, List<DockerContainerInfoModel> currentContainers)
    {
        var bringingComponentTag = installingVesrion_Component.FirstOrDefault(x => x[1].Contains(componentName.ToLower()))[0];
        var currentContainer = currentContainers.FirstOrDefault(x => x.ImageName == _imageStorage + componentName.ToLower());
        if (currentContainer == null)
        {
            return true;
        }

        var currentComponentOs = currentContainer.ImageTag.Substring(0, 13);
        var currentComponentVersion = currentContainer.ImageTag.Substring(13);
        var containerToKeep = currentContainer.ImageName + ":" + currentComponentOs + bringingComponentTag;

        if (bringingComponentTag == currentComponentVersion)
        {
            return false;
        }

        return true;
    }

    private void VerifySourcesPresent(Component[] components)
    {
        _logger.LogInformation("Verifying source files are present");
        var requiredFiles = components
            .SelectMany(component => component.Actions, (component, action) => new { component, action })
            .Where(t => t.action.Action is ComponentActionAction.Install or ComponentActionAction.Copy)
            .Where(t => !t.action.AdditionalProperties.TryGetValue("skipValidation", out var val) || ((JsonElement)val).GetBoolean() != true)
            .Select(t => Path.Combine(_registryConfig.Location, t.component.Name, t.action.Source))
            .Select(Path.GetFullPath)
            .ToArray();
        _logger.LogInformation($"Required installation files: {requiredFiles.StringJoin("; ")}");

        var missingFiles = requiredFiles
            .Where(location => VerifyFileExists(location) == false)
            .ToArray();

        if (missingFiles.Any())
        {
            throw new Exception($@"Required installation files are missing: {missingFiles.StringJoin("; " + Environment.NewLine)}");
        }
    }

    private static bool VerifyFileExists(string location)
    {
        var directory = Path.GetDirectoryName(location)!;
        var filename = Path.GetFileName(location);

        if (filename.Contains('*') || filename.Contains('?'))
        {
            var files = Directory.GetFiles(directory, filename);
            return files.Length > 0;
        }

        return File.Exists(location);
    }

    private async Task RunExecuteCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running execution for '{action.Source}'");
        var parts = action.Source.Split(' ');
        await _commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
    }

    private async Task RunInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running installation for '{action.Source}'");
        if (action.Source.EndsWith(".tar"))
        {
            var dockerPath = DockerPath.GetDockerPath();
            var policy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromMinutes(Math.Pow(2, retryAttempt)));
            await policy.ExecuteAsync(async () => await _commandExecutionService.ExecuteCommandAsync(dockerPath, $"load -i {action.Source}", rootLocation));
        }
        else
        {
            var parts = action.Source.Split(' ');
            await _commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
        }
    }

    private async Task RunUpdatePathCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Adding '{action.Source}' to system path");
        var path = Environment.GetEnvironmentVariable(Constants.PathEnvironmentVariable, EnvironmentVariableTarget.Machine)
                   ?? "";

        if (path.Contains(action.Source, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _commandExecutionService.ExecuteCommandAsync("setx", $"/M {Constants.PathEnvironmentVariable} \"%{Constants.PathEnvironmentVariable}%;{action.Source}\"", rootLocation);
    }

    private async Task RunCopyCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Copying file '{action.Source}' to '{action.Destination}'");
        var directory = Path.GetDirectoryName(action.Destination)!;

        if (string.IsNullOrWhiteSpace(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        await _commandExecutionService.ExecuteCommandAsync("copy", $"\"{action.Source}\" \"{action.Destination}\"", rootLocation);
    }

    private async Task RunReplaceParametersCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Replacing parameters in '{action.Source}'");
        var path = Path.Combine(rootLocation, action.Source);
        var text = await File.ReadAllTextAsync(path);

        foreach (var param in _allParameters)
        {
            text = text.Replace(param.Key, param.Value, StringComparison.OrdinalIgnoreCase);
        }

        await File.WriteAllTextAsync(path, text);
    }

    private async Task RunChunkCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        var chunker = new DataChunker(_loggerFactory);

        var source = new FileInfo(Path.Combine(rootLocation, action.Source));
        var destination = new DirectoryInfo(Path.Combine(rootLocation, action.Destination));
        await chunker.ChunkFileAsync(source, destination, DefaultMaxChunkSize, false);
    }

    private async Task RunUnchunkCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        var chunker = new DataChunker(_loggerFactory);

        var source = new DirectoryInfo(Path.Combine(rootLocation, action.Source));
        var destination = new FileInfo(Path.Combine(rootLocation, action.Destination));
        await chunker.UnchunkFileAsync(source, destination, false);
    }

    private async Task RunCompressCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        var compressor = new DataCompressor(_loggerFactory);

        var source = new DirectoryInfo(Path.Combine(rootLocation, action.Source));
        var destination = new FileInfo(Path.Combine(rootLocation, action.Destination));
        await compressor.CompressDirectoryAsync(source, destination, false);
    }

    private async Task RunUncompressCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        var compressor = new DataCompressor(_loggerFactory);

        var source = new FileInfo(Path.Combine(rootLocation, action.Source));
        var destination = new DirectoryInfo(Path.Combine(rootLocation, action.Destination));
        await compressor.UncompressFileAsync(source, destination, false);
    }

    private async Task RunDockerComposeCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running docker compose for '{action.Source}'");
        await ExecuteDockerComposeAsync(rootLocation, action);
    }

    private async Task UpdateRabbitCredentials(string usernameEnvVar,
        string passwordEnvVar,
        Models.AccountType accountType,
        string envFilePath,
        string updatingUserName,
        string rabbitDefinitionFile)
    {
        var adminUser = await GetRabbitAdminUser(rabbitDefinitionFile);

        var account = await RabbitConfigurer
                .ConfigureRabbitAsync(LocalRabbitLocation, adminUser.Name,
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
                _logger.LogInformation($"Updating {usernameEnvVar} in env file");
                userNameLineFound = true;
                envLines[i] = $"{usernameEnvVar}={account!.Username}";
            }

            if (envLines[i].Contains(passwordEnvVar))
            {
                _logger.LogInformation($"Updating {passwordEnvVar} in env file");
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
            _logger.LogInformation($"Adding {usernameEnvVar} to env file");
            envLines.Add($"{usernameEnvVar}={account!.Username}");
        }

        if (!userPassLineFound)
        {
            _logger.LogInformation($"Adding {passwordEnvVar} to env file");
            envLines.Add($"{passwordEnvVar}={account!.Password}");
        }

        var newText = envLines.StringJoin(Environment.NewLine);

        _logger.LogInformation("Updating env file {envFilePath}", envFilePath);
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

    private async Task RunPostDrexInstallCommandAsync(
        Component component,
        string _,
        ComponentAction action,
        Models.AccountType accountType)
    {
        try
        {
            _logger.LogInformation($"{component.Name}: Running Drex post install for '{action.Destination}'. Account {accountType}");
            _logger.LogInformation(LocalRabbitLocation.ToString());
            _logger.LogInformation(DrexSiteUsername);

            await UpdateRabbitCredentials(
                "DREX_SHARED_LOCAL_USERNAME",
                "DREX_SHARED_LOCAL_PASSWORD",
                accountType,
                action.Destination,
                DrexSiteUsername,
                action.Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Drex account");
            throw;
        }
    }

    private async Task RunPostRabbitMqInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running RabbitMq post install for '{action.Source}'");

        var configText = await File.ReadAllTextAsync(action.Source);
        var password = RabbitConfigurer.GeneratePassword();

        // Replace the guest password with a new one
        var newText = configText
            .RequireReplace("\"password\": \"guest\",", $"\"password\": \"{password}\",");

        _logger.LogInformation("Altering guest account");
        await File.WriteAllTextAsync(action.Source, newText);
    }

    private async Task RunPostDrexCentralInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        await RunPostDrexInstallCommandAsync(component, rootLocation, action, Models.AccountType.RemoteDrex);
    }

    private async Task RunPostDiscoInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running DISCO post install for '{action.Destination}'");
        await UpdateRabbitCredentials("DISCO_RABBIT_USERNAME", "DISCO_RABBIT_PASSWORD", Models.AccountType.Disco, action.Destination, DiscoSiteUsername, action.Source);
    }

    private async Task RunPostSiemensInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running Siemens post install for '{action.Destination}'");
        await UpdateRabbitCredentials("SIEMENS_RABBIT_USERNAME", "SIEMENS_RABBIT_PASSWORD", Models.AccountType.Siemens, action.Destination, SiemensSiteUsername, action.Source);
    }

    private async Task RunPostKdiInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running Kdi post install for '{action.Destination}'");
        await UpdateRabbitCredentials("KDI_RABBIT_USERNAME", "KDI_RABBIT_PASSWORD", Models.AccountType.Kdi, action.Destination, KdiSiteUsername, action.Source);
    }

    private async Task RunPostVectorInstallCommandAsync(
        Component component,
        string rootLocation,
        ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running Vector post install for '{action.Destination}'");
        var adminUser = await GetRabbitAdminUser(action.Source);

        var account = await RabbitConfigurer
            .ConfigureRabbitAsync(LocalRabbitLocation, adminUser.Name,
                                  adminUser.Password, VectorUsername, null,
                                  Models.AccountType.Vector, true);

        var config = await File.ReadAllTextAsync(action.Destination);

        // Replace the vector account credentials
        var newText = config
                      .RequireReplace($"{LocalRabbitUsername}:{LocalRabbitPassword}", $"{account!.Username}:{HttpUtility.UrlEncode(account.Password)}");

        _logger.LogInformation("Updating vector account");
        await File.WriteAllTextAsync(action.Destination, newText);
    }

    private async Task RunPostVoyageManagerInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running Voyage Report Manager post install for '{action.Destination}'");
        await UpdateRabbitCredentials("VOYAGE_MANAGER_RABBIT_USERNAME", "VOYAGE_MANAGER_RABBIT_PASSWORD", Models.AccountType.VMReport, action.Destination, VMReportUsername, action.Source);
    }

    private async Task RunPostMessageSchedulerInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running Message Scheduler post install for '{action.Destination}'");
        await UpdateRabbitCredentials("MESSAGE_SCHEDULER_USERNAME", "MESSAGE_SCHEDULER_PASSWORD", Models.AccountType.LocalDrex, action.Destination, MessageSchedulerUsername, action.Source);
    }

    private async Task RunPostInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running post install for '{action.Source}'");
        var parts = action.Source.Split(' ');
        await _commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
    }

    private async Task RunValidateJsonCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Validating JSON for '{action.Source}'");

        try
        {
            var schemaFilePath = action.AdditionalProperties["schema"].ToString();
            var jsonData = await File.ReadAllTextAsync(action.Source);
            var jsonSchemaData = await File.ReadAllTextAsync(schemaFilePath);

            var json = JToken.Parse(jsonData);
            var schema = JSchema.Parse(jsonSchemaData);

            var isValid = json.IsValid(schema, out IList<string> errorMessages);
            if (!isValid)
            {
                var errorMessage = $"'{action.Source}' validation failed: " +
                                   string.Join(Environment.NewLine, errorMessages);
                _logger.LogError(errorMessage);
                throw new Exception(errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"'{action.Source}' validation failed: {ex.Message}");
            throw;
        }
    }

    private async Task WaitForDockerContainersHealthyAsync(int totalContainers, TimeSpan totalTime, TimeSpan checkInterval)
    {
        _logger.LogInformation($"Waiting for {totalContainers} containers to be healthy");
        var start = DateTime.UtcNow;
        var client = new DockerClientConfiguration()
            .CreateClient();

        while (DateTime.UtcNow.Subtract(start) < totalTime)
        {
            var containers = await LoadContainerInfoAsync(client);
            var healthyCount = 0;

            if (containers.Length == 0)
            {
                _logger.LogWarning("No containers found");
            }

            foreach (var container in containers.OrderBy(x => x.Image))
            {
                var isHealthy = CheckContainerHealthy(container, TimeSpan.FromSeconds(30));
                if (isHealthy)
                {
                    healthyCount++;
                }

                var logLevel = isHealthy
                    ? Microsoft.Extensions.Logging.LogLevel.Information
                    : Microsoft.Extensions.Logging.LogLevel.Warning;

                _logger.Log(logLevel, $"Container '{container.Name.Trim('/')}': {(isHealthy ? "Healthy" : "Unhealthy")}");
            }

            if (healthyCount >= totalContainers)
            {
                _logger.LogInformation("All containers are healthy");
                return;
            }

            await Task.Delay(checkInterval);
        }

        _logger.LogError("Not all containers are healthy");
        throw new Exception("Not all containers are healthy");
    }

    private static async Task<ContainerInspectResponse[]> LoadContainerInfoAsync(DockerClient client)
    {
        var containers = await client.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

        var waitAndRetry = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt + 1)));

        var containerInfo =
            await containers.SelectAsync(async c =>
                await waitAndRetry.ExecuteAsync(async () => await client.Containers.InspectContainerAsync(c.ID)));

        return containerInfo.ToArray();
    }

    private static bool CheckContainerHealthy(ContainerInspectResponse container, TimeSpan containerHealthyTime)
    {
        var startTime = string.IsNullOrWhiteSpace(container.State.StartedAt)
            ? DateTime.MaxValue
            : DateTime.Parse(container.State.StartedAt).ToUniversalTime();

        return (container.State.Health == null || !string.Equals(container.State.Health.Status, "unhealthy", StringComparison.OrdinalIgnoreCase))
            && container.State.Running && container.State.Restarting == false &&
            ((container.State.Health != null && string.Equals(container.State.Health.Status, "healthy", StringComparison.OrdinalIgnoreCase)) ||
             DateTime.UtcNow.Subtract(startTime) > containerHealthyTime);
    }

    private async Task<bool> EnableContainersAsync()
    {
        await Task.Yield();
        _logger.LogInformation("Checking if windows containers are enabled");

        var mc = new ManagementClass(Win32OptionalFeatures);
        var containerFeature = mc.GetInstances()
            .OfType<ManagementBaseObject>()
            .FirstOrDefault(x =>
            {
                var propertyLookup = x.Properties
                    .OfType<PropertyData>()
                    .ToDictionary(x => x.Name, x => x.Value);

                propertyLookup.TryGetValue(Win32PropertyName, out var name);
                propertyLookup.TryGetValue(Win32InstalledStatus, out var installed);

                return name?.ToString() == Win32ContainerFeature && installed?.ToString() == Win32FeatureEnabled;
            });

        if (containerFeature is not null)
        {
            _logger.LogInformation("Containers are enabled");
            return true;
        }

        _logger.LogInformation("Containers are not enabled. Enabling to continue");

        //powershell -Command Enable-WindowsOptionalFeature -Online -FeatureName Containers -All -NoRestart
        await _commandExecutionService.ExecuteCommandAsync("powershell",
            "-Command Enable-WindowsOptionalFeature -Online -FeatureName Containers -All -NoRestart", "");

        _logger.LogInformation("Containers are enabled. Restart machine to continue");
        return false;
    }

    private async Task<string[]> PrintReadmeFileAsync()
    {
        var current = Directory.GetCurrentDirectory();
        var readmeName = Directory.GetFiles(current, "readme*.txt", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (string.IsNullOrEmpty(readmeName))
        {
            return new string[0];
        }

        var readmePath = Path.Combine(current, readmeName);
        var readmeExists = File.Exists(readmePath);

        var readmeLines = await File.ReadAllLinesAsync(readmePath);

        foreach (var line in readmeLines)
        {
            _logger.LogInformation(line);
        }

        return readmeLines;
    }

    private async Task ExpandReleaseZipFile()
    {
        _logger.LogInformation("Preparing install components");

        var current = Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(current, "*.zip*", SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            _logger.LogInformation("No release files found");
            return;
        }

        // Must stop docker first. Ours is dockerd, default can be docker
        //await _serviceManager.StopServiceAsync("dockerd");
        //await _serviceManager.StopServiceAsync("docker");

        var releaseZip = new FileInfo(Path.Combine(current, ReleaseZipName));
        var installLocation = new DirectoryInfo(_registryConfig.Location);

        if (!releaseZip.Exists)
        {
            _logger.LogInformation("Unchunking release files");
            var chunker = new DataChunker(_loggerFactory);
            await chunker.UnchunkFileAsync(new DirectoryInfo(current), releaseZip, false);
            if (!releaseZip.Exists)
            {
                var release = Directory.GetFiles(current, "*.zip", SearchOption.TopDirectoryOnly);
                if (release.Length == 1)
                {
                    releaseZip = new FileInfo(release[0]);
                    File.Copy(releaseZip.FullName, Path.Combine(current, ReleaseZipName));
                }
            }
        }
        else
        {
            _logger.LogInformation("Skip unchunking release files");
        }

        _logger.LogInformation("Uncompressing release file");
        var compressor = new DataCompressor(_loggerFactory);
        await compressor.UncompressFileAsync(releaseZip, installLocation, false);

        _logger.LogInformation($"Creating {AdditionalFilesName} folder");
        var path = Path.Combine(_registryConfig.Location, AdditionalFilesName);
        Directory.CreateDirectory(path);
    }

    private async Task StopAllContainersAsync()
    {
        var dockerPath = DockerPath.GetDockerPath();
        await _commandExecutionService.ExecuteCommandAsync("powershell",
                                                           $"-Command \"{dockerPath} stop $({dockerPath} ps -a -q)\" 2>&1", "");
    }

    private async Task DockerSystemPruneAsync()
    {
        var dockerPath = DockerPath.GetDockerPath();
        await _commandExecutionService.ExecuteCommandAsync(dockerPath, "system prune -f", "");
        await _commandExecutionService.ExecuteCommandAsync(dockerPath, "network prune -f", "");
    }

    private async Task ExecuteDockerComposeAsync(string rootLocation, ComponentAction action)
    {
        var configFiles = string.IsNullOrWhiteSpace(action.Destination)
            ? Directory.GetFiles(action.Source, "docker-compose.*.yml", SearchOption.AllDirectories)
            : [action.Destination, Path.Combine(action.Source, "docker-compose.root.yml")];

        var envFile = Directory.GetFiles(action.Source, "environment.env", SearchOption.TopDirectoryOnly);

        var arguments = configFiles
                        .Select(x => $"-f {x}")
                        .StringJoin(" ");

        if (envFile.Length == 1)
        {
            arguments = $"--env-file {envFile[0]} " + arguments;
        }

        var dockerComposePath = DockerPath.GetDockerComposePath();
        await _commandExecutionService.ExecuteCommandAsync(dockerComposePath, $"{arguments} up --build --detach 2>&1", rootLocation);

        var containerCount = configFiles
            .Count(x => !x.Contains(".root.", StringComparison.OrdinalIgnoreCase));

        if (WaitForDockerContainersHealthy)
        {
            await WaitForDockerContainersHealthyAsync(containerCount, TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(10));
        }
    }

    private async Task RequestCertificatesCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        var cloudClientId = action.AdditionalProperties["cloud_client_id"].ToString();
        var cloudClientSecret = action.AdditionalProperties["cloud_client_secret"].ToString();
        var cloudTenantId = action.AdditionalProperties["cloud_tenant_id"].ToString();
        var ccTenantId = action.AdditionalProperties["cc_tenant_id"].ToString();
        var centralHostName = action.AdditionalProperties["central_host_name"].ToString();
        var apimAppScope = "https://graph.microsoft.com/.default";
        var confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(cloudClientId)
            .WithClientSecret(cloudClientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{cloudTenantId}"))
            .Build();

        var builder = confidentialClientApplication.AcquireTokenForClient([apimAppScope]);
        var result = await builder.ExecuteAsync();
        var requestUrl = action.Source;

        using HttpClient client = new();
        client.BaseAddress = new Uri(requestUrl);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.MediaTypes.Protobuf));
        client.DefaultRequestHeaders.Add("Authorization", result.AccessToken);
        client.DefaultRequestHeaders.Add(Constants.Headers.Client, cloudClientId);
        client.DefaultRequestHeaders.Add(Constants.Headers.Tenant, ccTenantId);
        client.DefaultRequestHeaders.Add("central-hosts", $"[\"{centralHostName}\"]");

        var httpResponse = await client.GetAsync(requestUrl);
        var responseContent = await httpResponse.Content.ReadAsStreamAsync();

        if (httpResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation("Certificates received successfully");
            var certificates = CertificateResponse.Parser.ParseFrom(responseContent);
            var certificatePath = Path.Combine(rootLocation, action.Destination);
            Directory.CreateDirectory(certificatePath);

            await CreateCertificateFile(Path.Combine(certificatePath, "ca.pem"), certificates!.CaCerts);
            await CreateCertificateFile(Path.Combine(certificatePath, "central-rabbitmq.cert.pem"), certificates!.CentralServerCert);
            await CreateCertificateFile(Path.Combine(certificatePath, "central-rabbitmq.key.pem"), certificates!.CentralServerKeys);
            await CreateCertificateFile(Path.Combine(certificatePath, "vessel-rabbitmq.cert.pem"), certificates!.VesselShovelCert);
            await CreateCertificateFile(Path.Combine(certificatePath, "vessel-rabbitmq.key.pem"), certificates!.VesselShovelKeys);
            await CreateCertificateFile(Path.Combine(certificatePath, "cloud-rabbitmq.cert.pem"), certificates!.CentralShovelCert);
            await CreateCertificateFile(Path.Combine(certificatePath, "cloud-rabbitmq.key.pem"), certificates!.CentralShovelKeys);
            _logger.LogInformation("Certificates created successfully");
        }
        else
        {
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseContent);
            _logger.LogError("Failed to get certificates: {response}. {message}", responseContent,
                string.Join(Environment.NewLine, error?.Errors.Select(e => $"{e.Code.Code}:${e.Message}") ?? []));
            throw new Exception($"Failed to get certificates: {responseContent}");
        }

        return;

        async Task CreateCertificateFile(string path, string content)
        {
            if (File.Exists(path))
            {
                _logger.LogInformation("{path} already exists. Skipping...", path);
            }
            else
            {
                await File.WriteAllTextAsync(path, content);
                _logger.LogInformation("Created {path}", path);
            }
        }
    }
}
