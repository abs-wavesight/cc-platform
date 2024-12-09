using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Abs.CommonCore.Contracts.Json.Drex;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using PasswordGenerator;
using Polly;
using AccountType = Abs.CommonCore.Installer.Actions.Models.AccountType;
using User = EasyNetQ.Management.Client.Model.User;

namespace Abs.CommonCore.Installer.Actions;

public partial class RabbitConfigurer : ActionBase
{
    [GeneratedRegex(@"^cc\.drex\.(site|central)\.internal-src-dlq\.q$", RegexOptions.Compiled)]
    private static partial Regex InternalDlqRegex();

    [GeneratedRegex(@"^cc\.drex\.(site|central)\..*log.*\.q$", RegexOptions.Compiled)]
    private static partial Regex InternalRegex();

    [GeneratedRegex(@"^cc\.drex\.(ed|et)$", RegexOptions.Compiled)]
    private static partial Regex ExchangesRegex();

    [GeneratedRegex(@"^cc\.(drex\.site\.drex-file|drex-file\.site\.).*\.q$", RegexOptions.Compiled)]
    private static partial Regex SiteFileShippingRegex();

    private const string SystemVhost = "commoncore";
    private const string UsernamePlaceholder = "$USERNAME";
    private const string PasswordPlaceholder = "$PASSWORD";

    private readonly ILogger _logger;
    private readonly ICommandExecutionService _commandExecutionService;

    public RabbitConfigurer(ILoggerFactory loggerFactory, ICommandExecutionService commandExecutionService)
    {
        _commandExecutionService = commandExecutionService;
        _logger = loggerFactory.CreateLogger<RabbitConfigurer>();
    }

    public static async Task<RabbitCredentials?> ConfigureRabbitAsync(
        Uri rabbit,
        string rabbitUsername,
        string rabbitPassword,
        string username,
        string? password,
        AccountType accountType,
        bool isSilent)
    {
        Console.WriteLine($"Configuring RabbitMQ at '{rabbit}'");
        var waitAndRetry = Polly.Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(4, retryAttempt => retryAttempt switch
            {
                <= 3 => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt + 1)),
                _ => TimeSpan.FromMinutes(1)
            });

        var client = await waitAndRetry.ExecuteAsync(async () => new ManagementClient(rabbit, rabbitUsername, rabbitPassword));

        Console.WriteLine($"Connected to RabbitMQ at '{rabbit}'");
        return await ConfigureRabbitAsync(client, username, password, accountType, isSilent);
    }

    public static string GeneratePassword()
    {
        return new Password()
               .IncludeLowercase()
               .IncludeUppercase()
               .IncludeNumeric()
               .LengthRequired(32)
               .Next();
    }

    public static async Task UpdateUserPermissionsAsync(Uri rabbit, string rabbitUsername, string rabbitPassword, string username, AccountType accountType)
    {
        Console.WriteLine($"Updating user '{username}' permissions at '{rabbit}'");
        var client = new ManagementClient(rabbit, rabbitUsername, rabbitPassword);

        await UpdateUserPermissionsAsync(client, username, accountType);
    }

    public static async Task UpdateDrexSiteConfigAsync(FileInfo location, RabbitCredentials credentials)
    {
        Console.WriteLine($"Updating Drex site config at '{location}'");
        var config = ConfigParser.LoadConfig<DrexSiteConfig>(location.FullName);

        if (config.RemoteBuses.Count == 0)
        {
            throw new Exception("No connections to modify.");
        }

        if (config.RemoteBuses.Count != 1)
        {
            throw new Exception("Multiple connections found.");
        }

        config.RemoteBuses[0].Username = credentials.Username;
        config.RemoteBuses[0].Password = credentials.Password;

        await SaveConfigAsync(location, config);
    }

    public static async Task UpdateCredentialsFileAsync(RabbitCredentials credentials, FileInfo file)
    {
        Console.WriteLine($"Updating credentials file: {file.FullName}");

        var text = await File.ReadAllTextAsync(file.FullName);
        text = text
            .Replace(UsernamePlaceholder, credentials.Username)
            .Replace(PasswordPlaceholder, credentials.Password);

        await File.WriteAllTextAsync(file.FullName, text);

        Console.WriteLine("Credentials file updated");
    }

    private static async Task<RabbitCredentials?> ConfigureRabbitAsync(IManagementClient client, string username, string? password, AccountType accountType, bool isSilent)
    {
        var waitAndRetry = Polly.Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(4, retryAttempt => retryAttempt switch
            {
                <= 3 => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt + 1)),
                _ => TimeSpan.FromMinutes(1)
            });

        Console.WriteLine($"Checking if vhost '{SystemVhost}' exists");
        var existingVHosts = await waitAndRetry.ExecuteAsync(async () => await client.GetVhostsAsync());
        if (!existingVHosts.Any(v => string.Equals(v.Name, SystemVhost, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"Creating vhost '{SystemVhost}'");
            await client.CreateVhostAsync(SystemVhost);
        }

        // Cryptographically secure password generator: https://github.com/prjseal/PasswordGenerator/blob/0beb483fc6bf796bfa9f81db91265d74f90f29dd/PasswordGenerator/Password.cs#L157
        password = string.IsNullOrWhiteSpace(password)
            ? GeneratePassword()
            : password;

        Console.WriteLine($"Creating {accountType} account");
        var isAdded = await AddUserAccountAsync(client, username, password, accountType, isSilent);

        if (isAdded == false)
        {
            return null;
        }

        Console.WriteLine($"{accountType} account created.");
        Console.WriteLine($"User:     {username}");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine();

        return new RabbitCredentials
        {
            Username = username,
            Password = password,
        };
    }

    private static async Task<bool> AddUserAccountAsync(IManagementClient client, string username, string password, AccountType accountType, bool isSilent)
    {
        var waitAndRetry = Polly.Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(4, retryAttempt => retryAttempt switch
            {
                <= 3 => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt + 1)),
                _ => TimeSpan.FromMinutes(1)
            });
        Console.WriteLine($"Checking if user '{username}' exists");
        var existingUser = await waitAndRetry.ExecuteAsync(async () => await GetUserAsync(client, username));

        if (existingUser != null && string.Equals(existingUser.Name, username, StringComparison.OrdinalIgnoreCase) && !isSilent)
        {
            Console.WriteLine($"User '{username}' exists already. Continuing will change the user credentials.");
            Console.Write($"Type username '{username}' to continue: ");

            var response = Console.ReadLine();
            if (string.Equals(response, username, StringComparison.OrdinalIgnoreCase) == false)
            {
                Console.WriteLine("Aborting credential creation");
                return false;
            }
        }

        Console.WriteLine($"Creating user '{username}'");
        var user = UserInfo.ByPassword(password);
        if (accountType == AccountType.LocalDrex)
        {
            user = user.AddTag(UserTags.Administrator);
        }
        else
        {
            user = user.AddTag(UserTags.Management);
        }

        Console.WriteLine($"Sending request to create user '{username}'");
        await waitAndRetry.ExecuteAsync(async () => await client.CreateUserAsync(username, user));
        Console.WriteLine($"User '{username}' created");

        await waitAndRetry.ExecuteAsync(async () => await UpdateUserPermissionsAsync(client, username, accountType));

        var userRecord = await client.GetUserAsync(username);
        return userRecord != null
            ? true
            : throw new Exception("Unable to create user");
    }

    private static async Task UpdateUserPermissionsAsync(IManagementClient client, string username, AccountType accountType)
    {
        _ = await GetUserAsync(client, username)
            ?? throw new Exception("User account does not exist");

        var permissionRegex = BuildAccountPermissions(accountType, username);
        var configurePermissions = BuildConfigurePermissions(accountType, permissionRegex);

        var vHost = await client.GetVhostAsync(SystemVhost);
        await client.CreatePermissionAsync(vHost, username, new PermissionInfo(configurePermissions, permissionRegex, permissionRegex));
    }

    private static string BuildConfigurePermissions(AccountType accountType, string permissionsRegex)
    {
        return accountType switch
        {
            AccountType.Unknown => throw new Exception($"Unknown account type: {accountType}"),
            AccountType.LocalDrex => ".*",
            AccountType.RemoteDrex => ".*",
            AccountType.Disco => ".*",
            _ => permissionsRegex
        };
    }

    private static string BuildAccountPermissions(AccountType accountType, string username)
    {
        const string errorQueueName = "error";
        var internalDlqRegex = InternalDlqRegex().ToString();
        var internalRegex = InternalRegex().ToString();
        var exchangesRegex = ExchangesRegex().ToString();
        var siteFileShippingQueues = SiteFileShippingRegex().ToString();

        switch (accountType)
        {
            case AccountType.Unknown:
                throw new Exception($"Unknown account type: {accountType}");
            case AccountType.LocalDrex:
            case AccountType.RemoteDrex:
            case AccountType.Disco:
            case AccountType.VMReport:
                return ".*";
            case AccountType.Vector:
                const string siteQueue = Drex.Shared.Constants.MessageBus.Message.SiteLogQueueName;
                const string remoteQueue = Drex.Shared.Constants.MessageBus.Message.CentralLogQueueTemplate;
                return $"{exchangesRegex}|{errorQueueName}|{siteQueue}|{remoteQueue}";
            case AccountType.Siemens:
                const string discoTopicRegex = "cc\\.disco\\.et";
                const string discoDirectRegex = "cc\\.disco\\.ed";
                const string discoResponseRegex = "cc\\.disco\\.data\\.response\\..*\\.q";
                const string siemensRegex = "?=.*siemens\\.q";
                return $"^({siemensRegex}|{errorQueueName}|{discoDirectRegex}|{discoTopicRegex}|{discoResponseRegex}).*$";
            case AccountType.Kdi:
                const string kdiDiscoTopicRegex = "cc\\.disco\\.et";
                const string kdiDiscoDirectRegex = "cc\\.disco\\.ed";
                const string kdiDiscoResponseRegex = "cc\\.disco\\.data\\.response\\..*\\.q";
                const string kdiRegex = "?=.*kdi\\.q";
                return $"^({kdiRegex}|{errorQueueName}|{kdiDiscoDirectRegex}|{kdiDiscoTopicRegex}|{kdiDiscoResponseRegex}).*$";
        }

        var userQueuesRegex = @$".*(\.|\-)({Regex.Escape(username.ToLower())})(\.|\-).*";
        return new[] { userQueuesRegex, internalDlqRegex, internalRegex, exchangesRegex, errorQueueName, siteFileShippingQueues }
            .StringJoin("|");
    }

    private static async Task<User?> GetUserAsync(IManagementClient client, string username)
    {
        try
        {
            return await client.GetUserAsync(username);
        }
        catch (UnexpectedHttpStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"User '{username}' does not exist");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user '{username}': {ex.Message}");
            throw;
        }
    }

    private static async Task SaveConfigAsync<T>(FileInfo location, T config)
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            WriteIndented = true,
        };

        var json = JsonSerializer.Serialize(config, options);
        await File.WriteAllTextAsync(location.FullName, json);
    }
}
