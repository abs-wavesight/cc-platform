using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Abs.CommonCore.Platform;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebex;
using Rebex.Net;
using Rebex.Net.Servers;
using Rebex.Security.Certificates;

namespace Abs.CommonCore.SftpService;

[ExcludeFromCodeCoverage]
public class Program
{
    private const int SshKeyLength = 2048;
    private const string KeyFileName = "ssh_host_rsa_key";
    private const string KeyFingerprintFileName = "ssh-host-key-fingerprint.txt";
    private const SignatureHashAlgorithm FingerprintHash = SignatureHashAlgorithm.SHA512;

    public const string ConfigFolderPath = "config";
    public const string ConfigFileName = "config.json";

    public const string SshKeysFolderPath = "ssh-keys";
    public const string RootFolderPath = "sftproot";

    public static async Task<int> Main(string[]? args = null)
    {
        args ??= Array.Empty<string>();
        var runCommand = SetupRunCommand(args);
        var generateKeyCommand = SetupGenerateKeyCommand(args);

        var root = new RootCommand("Installer for the Common Core platform")
        {
            TreatUnmatchedTokensAsErrors = true
        };
        root.Add(runCommand);
        root.Add(generateKeyCommand);

        var result = await root.InvokeAsync(args);
        await Task.Delay(1000);
        return result;
    }

    private static Command SetupRunCommand(string[] args)
    {
        const string commandName = "run";
        var command = new Command(commandName)
        {
            TreatUnmatchedTokensAsErrors = true
        };

        command.SetHandler(async () => await ExecuteRunCommandAsync(args));
        return command;
    }

    private static Command SetupGenerateKeyCommand(string[] args)
    {
        const string commandName = "gen-key";
        var command = new Command(commandName)
        {
            TreatUnmatchedTokensAsErrors = true
        };

        const string pathLongParamName = "--path";
        const string pathShortParamName = "-p";
        var pathParam = new Option<DirectoryInfo>(pathLongParamName)
        {
            IsRequired = true
        };
        pathParam.AddAlias(pathShortParamName);
        command.Add(pathParam);

        command.SetHandler(async (path) => await ExecuteGenerateKeyCommandAsync(path, args), pathParam);
        return command;
    }

    private static async Task ExecuteRunCommandAsync(string[] args, CancellationToken cancellation = default)
    {
        var logger = Initialize(args);

        Rebex.Licensing.Key = PlatformConstants.Rebex_License_Key;

        logger.LogInformation("Creating file server");
        var server = new FileServer();

        server.Bind(1022, FileServerProtocol.Sftp);

        InitializeSettings(logger, server);
        await AddSshKeyAsync(logger, server);
        await AddUsersAsync(logger, server);
        SubscribeEvents(logger, server);

        server.Start();

        while (!cancellation.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellation);
        }
    }

    private static async Task ExecuteGenerateKeyCommandAsync(DirectoryInfo path, string[] args)
    {
        var logger = Initialize(args);
        logger.LogInformation($"Creating SSH key in folder '{path}'");

        var key = SshPrivateKey.Generate(SshHostKeyAlgorithm.RSA, SshKeyLength);

        var keyFile = Path.Combine(path.FullName, KeyFileName);
        key.Save(keyFile, null, SshPrivateKeyFormat.Pkcs8);

        var fingerprintFile = Path.Combine(path.FullName, KeyFingerprintFileName);
        var fingerprint = Convert.ToBase64String(key.Fingerprint.ToArray(FingerprintHash));
        await File.WriteAllTextAsync(fingerprintFile, fingerprint);

        logger.LogInformation("Ssh key created");
    }

    private static ILogger Initialize(string[] args)
    {
        var hostBuilder = Host.CreateApplicationBuilder(args);
        hostBuilder.Logging.ConfigureLogging(true);

        var serviceProvider = hostBuilder.Services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        return logger;
    }

    private static void InitializeSettings(ILogger logger, FileServer server)
    {
        server.LogWriter = new TeeLogWriter(new LogWriterAdapter(logger));
        server.Settings.AcceptWindowsPaths = false;
        server.Settings.ShowHiddenItems = false;
    }

    private static async Task AddSshKeyAsync(ILogger logger, FileServer server)
    {
        await Task.Yield();
        logger.LogInformation("Adding ssh key");
        var folderPath = LoadFolderPath(PlatformConstants.SSH_Keys_Path, SshKeysFolderPath);
        var keyFile = Path.Combine(folderPath, KeyFileName);

        server.Keys.Add(new SshPrivateKey(keyFile));
    }

    private static async Task AddUsersAsync(ILogger logger, FileServer server)
    {
        var root = LoadFolderPath(PlatformConstants.SFTP_Path, RootFolderPath);

        logger.LogInformation("Loading file server users");
        var users = await LoadUsersAsync();

        logger.LogInformation("Adding users");
        foreach (var user in users)
        {
            var userRoot = Path.Combine(root, user.Root);
            
            logger.LogInformation($"Adding user '{user.Name}' with root '{userRoot}'");
            Directory.CreateDirectory(userRoot);
            server.Users.Add(user.Name, user.Password, userRoot);
        }
    }

    private static async Task<SftpUser[]> LoadUsersAsync()
    {
        var configPath = Path.Combine(ConfigFolderPath, ConfigFileName);
        if (!File.Exists(configPath))
        {
            throw new Exception("Config file not found");
        }

        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<Configuration>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

        var clients = config!.Clients
                             .Select(x => new SftpUser { Name = x, Password = config.DefaultPassword, Root = x });

        var sites = config!.Sites
                           .Select(x => new SftpUser { Name = x.Username, Password = x.Password, Root = "" });

        return clients
               .Concat(sites)
               .ToArray();
    }

    private static void SubscribeEvents(ILogger logger, FileServer server)
    {
        server.ErrorOccurred += (_, args) => logger.LogError(args.Error, "Sftp error");
    }

    private static string LoadFolderPath(string environmentVariable, string? defaultValue = null)
    {
        return Environment.GetEnvironmentVariable(environmentVariable) ??
               (!string.IsNullOrWhiteSpace(defaultValue)
                   ? defaultValue
                   : throw new Exception($"Unable to find environment variable 'environmentVariable'"));
    }
}
