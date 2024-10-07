using System.Management;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Contracts.Proto.Cloud.VoyageManager;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Extensions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Abs.CommonCore.RabbitMQ.Shared.Models;
using Abs.CommonCore.RabbitMQ.Shared.Models.Exchange;
using Abs.CommonCore.RabbitMQ.Shared.Services;
using Docker.DotNet;
using Docker.DotNet.Models;
using Google.Protobuf;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using JsonParser = Abs.CommonCore.Installer.Services.JsonParser;

#pragma warning disable CA1416
namespace Abs.CommonCore.Installer.Actions;

public class ComponentInstaller : ActionBase
{
    private readonly Uri _localRabbitLocation = new("https://localhost:15671");
    private const string LocalRabbitUsername = "guest";
    private const string LocalRabbitPassword = "guest";
    private const string DrexSiteUsername = "drex";
    private const string VectorUsername = "vector";
    private const string DockerServiceName = "dockerd";
    private const string DiscoSiteUsername = "disco";
    private const string SiemensSiteUsername = "siemens-adapter";
    private const string KdiSiteUsername = "kdi-adapter";

    private const int DefaultMaxChunkSize = 1 * 1024 * 1024 * 1024; // 1GB
    private const string ReleaseZipName = "Release.zip";
    private const string ReadmeName = "readme.txt";
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
    private string _generatedGuestPassword = "guest";

    public bool WaitForDockerContainersHealthy { get; set; } = true;

    public ComponentInstaller(ILoggerFactory loggerFactory, ICommandExecutionService commandExecutionService, IServiceManager serviceManager,
        FileInfo registryConfig, FileInfo? installerConfig, Dictionary<string, string> parameters, bool promptForMissingParameters)
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

        await PrintReadmeFileAsync();
        await ExpandReleaseZipFile();

        var components = DetermineComponents(specificComponents);
        VerifySourcesPresent(components);

        var actions = components
            .Select(x => new { Component = x, x.Actions })
            .SelectMany(x => x.Actions.Select(y => new
            {
                x.Component,
                RootLocation = Path.Combine(_registryConfig.Location, x.Component.Name),
                Action = y
            }))
            .OrderByDescending(x => x.Action.Action == ComponentActionAction.Copy)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.ValidateJson)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.ReplaceParameters)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.ExecuteImmediate)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.Install)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.Execute)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.UpdatePath)
            .ThenByDescending(x =>
                x.Action.Action is ComponentActionAction.Chunk or
                    ComponentActionAction.Unchunk or
                    ComponentActionAction.Compress or
                    ComponentActionAction.Uncompress)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.RunDockerCompose)
            .ThenByDescending(x =>
                x.Action.Action is ComponentActionAction.PostDrexInstall or
                    ComponentActionAction.PostRabbitMqInstall or
                    ComponentActionAction.PostVectorInstall or
                    ComponentActionAction.PostDiscoInstall or
                    ComponentActionAction.PostSiemensInstall or
                    ComponentActionAction.PostKdiInstall)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.PostInstall)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.SystemRestore)
            .ThenByDescending(x => x.Action.Action == ComponentActionAction.CreateShovel)
            .ToArray();

        foreach (var action in actions)
        {
            await ProcessExecuteActionAsync(action.Component, action.RootLocation, action.Action);
        }

        _logger.LogInformation("Installer complete");
    }

    public async Task RunSystemRestoreCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running system restore for '{action.Source}'");
        await _serviceManager.StartServiceAsync(DockerServiceName);

        await StopAllContainersAsync();
        await DockerSystemPruneAsync();

        await ExecuteDockerComposeAsync(rootLocation, action);
    }

    private Component[] DetermineComponents(string[]? specificComponents)
    {
        try
        {
            if (specificComponents?.Length > 0)
            {
                return specificComponents
                    .Select(x => _registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
            }

            if (_installerConfig?.Components.Count > 0)
            {
                return _installerConfig.Components
                    .Select(x => _registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to determine components to use", ex);
        }

        throw new Exception("No components found to download");
    }

    private void VerifySourcesPresent(Component[] components)
    {
        _logger.LogInformation("Verifying source files are present");
        var requiredFiles = components
            .SelectMany(component => component.Actions, (component, action) => new { component, action })
            .Where(t => t.action.Action is ComponentActionAction.Install or ComponentActionAction.Copy)
            .Select(t => Path.Combine(_registryConfig.Location, t.component.Name, t.action.Source))
            .Select(Path.GetFullPath)
            .ToArray();
        _logger.LogInformation($"Required installation files: {requiredFiles.StringJoin("; ")}");

        var missingFiles = requiredFiles
            .Where(location => VerifyFileExists(location) == false)
            .ToArray();

        if (missingFiles.Any())
        {
            throw new Exception($@"Required installation files are missing: 
{missingFiles.StringJoin("; " + Environment.NewLine)}");
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
                ComponentActionAction.PostDrexInstall => RunPostDrexInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostVectorInstall => RunPostVectorInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostRabbitMqInstall => RunPostRabbitMqInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostInstall => RunPostInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostDiscoInstall => RunPostDiscoInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostSiemensInstall => RunPostSiemensInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.PostKdiInstall => RunPostKdiInstallCommandAsync(component, rootLocation, action),
                ComponentActionAction.SystemRestore => RunSystemRestoreCommandAsync(component, rootLocation, action),
                ComponentActionAction.CreateShovel => RunShovelCreationCommandAsync(component, action),
                ComponentActionAction.ValidateJson => RunValidateJsonCommandAsync(component, rootLocation, action),
                _ => throw new Exception($"Unknown action command: {action.Action}")
            });
        }
        catch (Exception ex)
        {
            var message = $"Unable to process install action. {JsonSerializer.Serialize(action)}";
            throw new Exception(message, ex);
        }
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
            await _commandExecutionService.ExecuteCommandAsync(dockerPath, $"load -i {action.Source}", rootLocation);
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

    private async Task RunPostDrexInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running Drex post install for '{action.Source}'");

        var account = await RabbitConfigurer
            .ConfigureRabbitAsync(_localRabbitLocation, LocalRabbitUsername,
                                  LocalRabbitPassword, DrexSiteUsername, null,
                                  Models.AccountType.LocalDrex, true);

        const string usernameVar = "DREX_SHARED_LOCAL_USERNAME";
        const string passwordVar = "DREX_SHARED_LOCAL_PASSWORD";

        var envFile = await File.ReadAllTextAsync(action.Source);

        // Replace the drex local account credentials
        var newText = envFile
            .RequireReplace($"{usernameVar}={LocalRabbitUsername}", $"{usernameVar}={account!.Username}")
            .RequireReplace($"{passwordVar}={LocalRabbitPassword}", $"{passwordVar}={account.Password}");

        _logger.LogInformation("Updating local drex account");
        await File.WriteAllTextAsync(action.Source, newText);
    }

    private async Task RunPostRabbitMqInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running RabbitMq post install for '{action.Source}'");

        var configText = await File.ReadAllTextAsync(action.Source);
        var password = RabbitConfigurer.GeneratePassword();

        // Replace the guest password with a new one
        var newText = configText
            .RequireReplace("\"password\": \"guest\",", $"\"password\": \"{password}\",");

        _generatedGuestPassword = password;
        _logger.LogInformation("Altering guest account");
        await File.WriteAllTextAsync(action.Source, newText);
    }

    private async Task RunShovelCreationCommandAsync(Component component, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running shovel creation.");
        var vHostName = "voyagemgr";
        var outcomingExchangeName = "eh.cccp.central.outgoing";
        var outcomingQueueName = "q.cccp.central.outgoing";
        var incomingExchangeName = "eh.cccp.central.incoming";
        var incomingQueueName = "q.cccp.central.incoming";
        var localRmqConfiguration = new RmqConfiguration
        {
            RmqHost = _localRabbitLocation.Authority,
            RmqUserName = "guest",
            RmqPassword = _generatedGuestPassword,
            RmqVirtualHost = vHostName,
        };

        var vhostServices = new RmqVhostService(localRmqConfiguration, new HttpClient(), _logger, "https");
        var vhosts = await vhostServices.GetVhostAsync().Result.ToListAsync();
        if (!vhosts.Any(vh => vh.Name == vHostName))
        {
            await vhostServices.CreateVhostAsync("voyagemgr", "Dedicated to create resources for communication with cloud services", "cloud");
        }

        var username = "clouduser";
        var password = RabbitConfigurer.GeneratePassword();

        var userServices = new RmqUserService(localRmqConfiguration, new HttpClient(), _logger, "https");
        var users = await userServices.GetUsersAsync();
        var userList = await users.ToListAsync();
        if (!userList.Any(vh => vh.Name == username))
        {
            await userServices.CreateUserAsync(username, password, "");
        }

        var queueServices = new RmqQueueService(localRmqConfiguration, new HttpClient(), _logger, "https");
        var exchangeServices = new RmqExchangeService(localRmqConfiguration, new HttpClient(), _logger, "https");
        var bindingServices = new RmqBindingService(localRmqConfiguration, new HttpClient(), _logger, "https");
        var outgoingModel = new QueueExchangeBindingModel
        {
            ExchangeName = outcomingExchangeName,
            QueueName = outcomingQueueName,
            ExchangeType = ExchangeType.Headers,
        };
        var incomingModel = new QueueExchangeBindingModel
        {
            ExchangeName = incomingExchangeName,
            QueueName = incomingQueueName,
            ExchangeType = ExchangeType.Headers,
        };

        var resourcesAreCreated = await UtilityServices.CreateBindedQueueAndExchangeAsync(queueServices, bindingServices, exchangeServices, outgoingModel, _logger);
        resourcesAreCreated = resourcesAreCreated && await UtilityServices.CreateBindedQueueAndExchangeAsync(queueServices, bindingServices, exchangeServices, incomingModel, _logger);

        if (resourcesAreCreated)
        {
            _logger.LogInformation("Queue and exchange are created successfully.");
        }
        else
        {
            _logger.LogError("Queue and exchange creation failed.");
            return;
        }

        var configText = await File.ReadAllTextAsync(action.Source);

        var newText = configText
            .RequireReplace("\"user\": \"guest\",", $"\"user\": \"{username}\",");
        newText = newText
            .RequireReplace("\"password\": \"guest\"", $"\"password\": \"{password}\"");

        var parameters = JsonSerializer.Deserialize<CloudParameters>(newText);

        var apimAppScope = $"https://graph.microsoft.com/.default";
        var confidentialClientApplication = ConfidentialClientApplicationBuilder
        .Create(parameters.CloudClientId)
            .WithClientSecret(parameters.CloudClientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{parameters.CloudTenantId}"))
            .Build();

        var builder = confidentialClientApplication.AcquireTokenForClient(new[] { apimAppScope });
        var result = await builder.ExecuteAsync();
        var requestUrl = $"{parameters.ApimServiceUrl}/vmprovisionadapters/CentralRegistry";
        var client = new HttpClient();
        client.BaseAddress = new Uri(requestUrl);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.MediaTypes.Protobuf));
        client.DefaultRequestHeaders.Add("Authorization", result.AccessToken);
        client.DefaultRequestHeaders.Add(Constants.Headers.Client, parameters.CloudClientId);

        var requestBody = new RegistrationRequest
        {
            Username = username,
            Password = password,
            RmqHostname = parameters.CentralHostName,
            RmqVirtualHost = vHostName,
            RmqPort = 15671,
            CcTenantId = parameters.CcTenantId,
            IncomingExchangeName = incomingExchangeName,
            OutgoingExchangeName = outcomingExchangeName,
            IncomingQueueName = incomingQueueName,
            OutgoingQueueName = outcomingQueueName,
        };

        var body = new MemoryStream();
        body.Write(requestBody.ToByteArray());
        body.Position = 0;

        var httpRequest = new HttpRequestMessage();

        var requestContent = new StreamContent(body);
        requestContent.Headers.Add(Constants.Headers.ContentType, Constants.MediaTypes.Protobuf);

        var httpResponse = await client.PostAsync(requestUrl, requestContent);

        if (httpResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation("Shovel is created successfully.");
        }
        else
        {
            await exchangeServices.DeleteExchangeAsync(outcomingExchangeName);
            await exchangeServices.DeleteExchangeAsync(incomingExchangeName);
            await queueServices.DeleteQueueAsync(outcomingQueueName);
            await queueServices.DeleteQueueAsync(incomingQueueName);
            await userServices.DeleteUserAsync(username);
        }
    }

    private async Task RunPostDiscoInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running DISCO post install for '{action.Source}'");

        var account = await RabbitConfigurer
            .ConfigureRabbitAsync(_localRabbitLocation, LocalRabbitUsername,
                                  LocalRabbitPassword, DiscoSiteUsername, null,
                                  Models.AccountType.Disco, true);

        const string usernameVar = "DISCO_RABBIT_USERNAME";
        const string passwordVar = "DISCO_RABBIT_PASSWORD";

        var configText = await File.ReadAllTextAsync(action.Source);

        // Replace the default password with a new one
        var newText = configText
            .RequireReplace($"{usernameVar}={LocalRabbitUsername}", $"{usernameVar}={account!.Username}")
            .RequireReplace($"{passwordVar}={LocalRabbitPassword}", $"{passwordVar}={account.Password}");

        _logger.LogInformation("Altering default account");
        await File.WriteAllTextAsync(action.Source, newText);
    }

    private async Task RunPostSiemensInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running Siemens post install for '{action.Source}'");

        var account = await RabbitConfigurer
            .ConfigureRabbitAsync(_localRabbitLocation, LocalRabbitUsername,
                                  LocalRabbitPassword, SiemensSiteUsername, null,
                                  Models.AccountType.Siemens, true);

        const string usernameVar = "SIEMENS_RABBIT_USERNAME";
        const string passwordVar = "SIEMENS_RABBIT_PASSWORD";

        var configText = await File.ReadAllTextAsync(action.Source);

        // Replace the default password with a new one
        var newText = configText
            .RequireReplace($"{usernameVar}={LocalRabbitUsername}", $"{usernameVar}={account!.Username}")
            .RequireReplace($"{passwordVar}={LocalRabbitPassword}", $"{passwordVar}={account!.Password}");

        _logger.LogInformation("Altering default account");
        await File.WriteAllTextAsync(action.Source, newText);
    }

    private async Task RunPostKdiInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running Kdi post install for '{action.Source}'");

        var account = await RabbitConfigurer
            .ConfigureRabbitAsync(_localRabbitLocation, LocalRabbitUsername,
                                  LocalRabbitPassword, KdiSiteUsername, null,
                                  Models.AccountType.Kdi, true);

        const string usernameVar = "KDI_RABBIT_USERNAME";
        const string passwordVar = "KDI_RABBIT_PASSWORD";

        var configText = await File.ReadAllTextAsync(action.Source);

        // Replace the default password with a new one
        var newText = configText
            .RequireReplace($"{usernameVar}={LocalRabbitUsername}", $"{usernameVar}={account!.Username}")
            .RequireReplace($"{passwordVar}={LocalRabbitPassword}", $"{passwordVar}={account!.Password}");

        _logger.LogInformation("Altering default account");
        await File.WriteAllTextAsync(action.Source, newText);
    }

    private async Task RunPostVectorInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        _logger.LogInformation($"{component.Name}: Running Vector post install for '{action.Source}'");

        var account = await RabbitConfigurer
            .ConfigureRabbitAsync(_localRabbitLocation, LocalRabbitUsername,
                                  LocalRabbitPassword, VectorUsername, null,
                                  Models.AccountType.Vector, true);

        var config = await File.ReadAllTextAsync(action.Source);

        // Replace the vector account credentials
        var newText = config
                      .RequireReplace($"{LocalRabbitUsername}:{LocalRabbitPassword}", $"{account!.Username}:{HttpUtility.UrlEncode(account.Password)}");

        _logger.LogInformation("Updating vector account");
        await File.WriteAllTextAsync(action.Source, newText);
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

            if (healthyCount == totalContainers)
            {
                _logger.LogInformation("All containers are healthy");
                return;
            }

            await Task.Delay(checkInterval);
        }

        throw new Exception("Not all containers are healthy");
    }

    private static async Task<ContainerInspectResponse[]> LoadContainerInfoAsync(DockerClient client)
    {
        var containers = await client.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

        var containerInfo = await containers
            .SelectAsync(async c => await client.Containers.InspectContainerAsync(c.ID));

        return containerInfo
            .ToArray();
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

    private async Task PrintReadmeFileAsync()
    {
        var current = Directory.GetCurrentDirectory();
        var readmePath = Path.Combine(current, ReadmeName);
        var readmeExists = File.Exists(readmePath);

        if (!readmeExists)
        {
            return;
        }

        var readmeLines = await File.ReadAllLinesAsync(readmePath);

        foreach (var line in readmeLines)
        {
            _logger.LogInformation(line);
        }
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
        await _serviceManager.StopServiceAsync("dockerd");
        await _serviceManager.StopServiceAsync("docker");

        var releaseZip = new FileInfo(Path.Combine(current, ReleaseZipName));
        var installLocation = new DirectoryInfo(_registryConfig.Location);

        if (!releaseZip.Exists)
        {
            _logger.LogInformation("Unchunking release files");
            var chunker = new DataChunker(_loggerFactory);
            await chunker.UnchunkFileAsync(new DirectoryInfo(current), releaseZip, false);
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
    }

    private async Task ExecuteDockerComposeAsync(string rootLocation, ComponentAction action)
    {
        var configFiles = Directory.GetFiles(action.Source, "docker-compose.*.yml", SearchOption.AllDirectories);
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
}
