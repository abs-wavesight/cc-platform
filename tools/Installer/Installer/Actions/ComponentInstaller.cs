using System.Management;
using System.Net.Http.Headers;
using System.Text.Json;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Contracts.Proto;
using Abs.CommonCore.Contracts.Proto.Installer;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Extensions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Polly;
using JsonParser = Abs.CommonCore.Installer.Services.JsonParser;

#pragma warning disable CA1416
namespace Abs.CommonCore.Installer.Actions;

public class ComponentInstaller : ActionBase
{
    private const string DockerServiceName = "dockerd";
    private const string _imageStorage = "ghcr.io/abs-wavesight/";
    
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
    private readonly IDockerActions _dockerActions;
    private readonly ComponentDeterminationSteps _componentDeterminationSteps;
    private readonly FileManipulationSteps _fileManipulationSteps;
    private readonly PostInstallActions _postInstallActions;
    private readonly PreInstallActions _preInstallActions;
    private readonly InstallerComponentInstallerConfig? _installerConfig;
    private readonly InstallerComponentRegistryConfig _registryConfig;
    private readonly Dictionary<string, string> _allParameters;

    public ComponentInstaller(
        ILoggerFactory loggerFactory,
        ICommandExecutionService commandExecutionService,
        IServiceManager serviceManager,
        FileInfo registryConfig,
        FileInfo? installerConfig,
        Dictionary<string, string> parameters,
        bool promptForMissingParameters,
        IDockerActions dockerActions)
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

        _dockerActions = dockerActions;
        _componentDeterminationSteps = new ComponentDeterminationSteps(commandExecutionService, _logger, serviceManager, _registryConfig, _installerConfig, _imageStorage);
        _fileManipulationSteps = new FileManipulationSteps(_logger, loggerFactory, commandExecutionService, _registryConfig);
        _postInstallActions = new PostInstallActions(_logger, commandExecutionService);
        _preInstallActions = new PreInstallActions(commandExecutionService, _logger, _componentDeterminationSteps);
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
        var readmeLines = await _fileManipulationSteps.PrintReadmeFileAsync();
        await _fileManipulationSteps.ExpandReleaseZipFile();
        var cleaningScriptPath = _commandExecutionService.GetCleaningScriptPath(_registryConfig.Location);

        var imageListTxtPath = Path.Combine(cleaningScriptPath, "image_list.txt");
        if (File.Exists(imageListTxtPath))
        {
            File.Delete(imageListTxtPath);
        }

        var dockerRun = await _dockerActions.IsDockerRunning(dockerPath);

        var widowsVersionSpecified = false;
        string[][] installingVersionComponent = null!;
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
            installingVersionComponent = readmeLines.Skip(3).Select(c => c.Split(":")[1].Trim().Split("_")).ToArray();
            var resultContainers = installingVersionComponent.Select(x => $"{_imageStorage}{x[1]}:windows-{windowsVersion}-{x[0]}").ToArray();
            await File.WriteAllLinesAsync(imageListTxtPath, resultContainers);

            if (dockerRun)
            {
                await _commandExecutionService.ExecuteCommandAsync("powershell", $"-File cleanup.ps1 -DockerPath {dockerPath}", cleaningScriptPath);
            }
        }

        var components = await _componentDeterminationSteps.DetermineComponents(specificComponents, installingVersionComponent, dockerRun);
        _fileManipulationSteps.VerifySourcesPresent(components);

        var orderedComponenets = components
            .OrderByDescending(x => x.Name == "Installer")
            .ThenByDescending(x => x.Name == "Check-Observability-Prerequisites")
            .ThenByDescending(x => x.Name == "Docker")
            .ThenByDescending(x => x.Name is "Certificates-Central" or "Certificates-Site")
            .ThenByDescending(x => x.Name == "OpenSsl")
            .ThenByDescending(x => x.Name == "RabbitMq")
            .ThenByDescending(x => x.Name == "Vector")
            .ThenByDescending(x => x.Name == "Sftp-Service")
            .ThenByDescending(x => x.Name is "Drex-Message" or "Drex-Central-Message")
            .ThenByDescending(x => x.Name is "Drex-File" or "Voyage-Manager-Report-Adapter" or "Message-Scheduler" or "Drex-Notification-Adapter")
            .ThenByDescending(x => x.Name == "Disco")
            .ThenByDescending(x => x.Name is "Siemens" or "Kdi")
            .ThenByDescending(x => x.Name == "Observability-Service")
            .ToArray();

        foreach (var component in orderedComponenets)
        {
            var orderedActions = component.Actions
            .OrderByDescending(x => x.Action == ComponentActionAction.CheckPrerequisites)
            .ThenByDescending(x => x.Action == ComponentActionAction.RequestCertificates)
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
                ComponentActionAction.CheckPrerequisites => _preInstallActions.CheckRequiredContainersRun(component, rootLocation, action, await _dockerActions.IsDockerRunning(DockerPath.GetDockerPath())),
                ComponentActionAction.Execute => RunExecuteCommandAsync(component, rootLocation, action),
                ComponentActionAction.ExecuteImmediate => RunExecuteCommandAsync(component, rootLocation, action),
                ComponentActionAction.Copy => _fileManipulationSteps.RunCopyCommandAsync(component, rootLocation, action),
                ComponentActionAction.Install => RunInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.UpdatePath => _fileManipulationSteps.RunUpdatePathCommandAsync(component, rootLocation, action),
                ComponentActionAction.ReplaceParameters => _fileManipulationSteps.RunReplaceParametersCommandAsync(component, rootLocation, action, _allParameters),
                ComponentActionAction.Chunk => _fileManipulationSteps.RunChunkCommandAsync(component, rootLocation, action),
                ComponentActionAction.Unchunk => _fileManipulationSteps.RunUnchunkCommandAsync(component, rootLocation, action),
                ComponentActionAction.Compress => _fileManipulationSteps.RunCompressCommandAsync(component, rootLocation, action),
                ComponentActionAction.Uncompress => _fileManipulationSteps.RunUncompressCommandAsync(component, rootLocation, action),
                ComponentActionAction.RunDockerCompose => _dockerActions.RunDockerComposeCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostDrexInstall => _postInstallActions.RunPostDrexInstallCommandAsync(component, rootLocation, action, AccountType.LocalDrex, _allParameters.GetInstallationEnvironment()),
                ComponentActionAction.PostVectorInstall => _postInstallActions.RunPostVectorInstallCommandAsync(component, rootLocation, action, _allParameters.GetInstallationEnvironment()),
                ComponentActionAction.PostRabbitMqInstall => _postInstallActions.RunPostRabbitMqInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostVMReportInstall => _postInstallActions.RunPostVoyageManagerInstallCommandAsync(component, rootLocation, action, _allParameters.GetInstallationEnvironment()),
                ComponentActionAction.PostMessageSchedulerInstall => _postInstallActions.RunPostMessageSchedulerInstallCommandAsync(component, rootLocation, action, _allParameters.GetInstallationEnvironment()),
                ComponentActionAction.PostInstall => _postInstallActions.RunPostInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostDiscoInstall => _postInstallActions.RunPostDiscoInstallCommandAsync(component, rootLocation, action, _allParameters.GetInstallationEnvironment()),
                ComponentActionAction.PostSiemensInstall => _postInstallActions.RunPostSiemensInstallCommandAsync(component, rootLocation, action, _allParameters.GetInstallationEnvironment()),
                ComponentActionAction.PostKdiInstall => _postInstallActions.RunPostKdiInstallCommandAsync(component, rootLocation, action, _allParameters.GetInstallationEnvironment()),
                ComponentActionAction.SystemRestore => RunSystemRestoreCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostDrexCentralInstall => _postInstallActions.RunPostDrexCentralInstallCommandAsync(component, rootLocation, action, _allParameters.GetInstallationEnvironment()),
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

        await _dockerActions.StopAllContainersAsync();
        await _dockerActions.DockerSystemPruneAsync();

        await _dockerActions.ExecuteDockerComposeAsync(rootLocation, action);
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
            var content = await httpResponse.Content.ReadAsStringAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(content);
            _logger.LogError("Failed to get certificates: {response}. {message}", content,
                string.Join(Environment.NewLine, error?.Errors.Select(e => $"{e.Code.Code}:${e.Message}") ?? []));
            throw new Exception($"Failed to get certificates: {content}");
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
