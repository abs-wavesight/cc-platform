using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Abs.CommonCore.Contracts.Json.Drex;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Logging;
using PasswordGenerator;

namespace Abs.CommonCore.Installer.Actions
{
    public class RabbitConfigurer
    {
        private const string SystemVhost = "/";
        private const string LocalMessageBusUsername = "LocalMessageBus__Username";
        private const string LocalMessageBusPassword = "LocalMessageBus__Password";


        private readonly ILogger _logger;
        private readonly ICommandExecutionService _commandExecutionService;

        public RabbitConfigurer(ILoggerFactory loggerFactory, ICommandExecutionService commandExecutionService)
        {
            _commandExecutionService = commandExecutionService;
            _logger = loggerFactory.CreateLogger<RabbitConfigurer>();
        }

        public async Task<RabbitCredentials?> ConfigureRabbitAsync(Uri rabbit, string rabbitUsername, string rabbitPassword, string username, string? password, bool isSuperUser)
        {
            Console.WriteLine($"Configuring RabbitMQ at '{rabbit}'");
            var client = new ManagementClient(rabbit, rabbitUsername, rabbitPassword);

            return await ConfigureRabbitAsync(client, username, password, isSuperUser);
        }

        public async Task UpdateUserPermissionsAsync(Uri rabbit, string rabbitUsername, string rabbitPassword, string username, bool isSuperUser)
        {
            Console.WriteLine($"Updating user '{username}' permissions at '{rabbit}'");
            var client = new ManagementClient(rabbit, rabbitUsername, rabbitPassword);

            await UpdateUserPermissionsAsync(client, username, isSuperUser);
        }

        public async Task UpdateDrexSiteConfigAsync(FileInfo location, RabbitCredentials credentials)
        {
            Console.WriteLine($"Updating Drex site config at '{location}'");
            var config = ConfigParser.LoadConfig<DrexSiteConfig>(location.FullName);

            if (config.RemoteBuses.Count == 0) throw new Exception("No connections to modify.");
            if (config.RemoteBuses.Count != 1) throw new Exception("Multiple connections found.");

            config.RemoteBuses[0].Username = credentials.Username;
            config.RemoteBuses[0].Password = credentials.Password;

            await SaveConfigAsync(location, config);
        }

        public async Task UpdateDrexEnvironmentVariablesAsync(RabbitCredentials credentials)
        {
            Console.WriteLine("Updating Drex environment variables with credentials");

            await _commandExecutionService.ExecuteCommandAsync("setx", $"/M {LocalMessageBusUsername} \"{credentials.Username}\"", "");
            await _commandExecutionService.ExecuteCommandAsync("setx", $"/M {LocalMessageBusPassword} \"{credentials.Password}\"", "");

            Console.WriteLine("Environment variables updated");
        }

        private async Task<RabbitCredentials?> ConfigureRabbitAsync(IManagementClient client, string username, string? password, bool isSuperUser)
        {
            // Cryptographically secure password generator: https://github.com/prjseal/PasswordGenerator/blob/0beb483fc6bf796bfa9f81db91265d74f90f29dd/PasswordGenerator/Password.cs#L157
            password = string.IsNullOrWhiteSpace(password)
                ? new Password(true, true, true, true, 32)
                    .Next()
                : password;

            var isAdded = await AddUserAccountAsync(client, username, password, isSuperUser);

            if (isAdded == false)
            {
                return null;
            }

            Console.WriteLine("User account created.");
            Console.WriteLine($"User:     {username}");
            Console.WriteLine($"Password: {password}");
            Console.WriteLine();

            return new RabbitCredentials
            {
                Username = username,
                Password = password,
            };
        }

        private async Task<bool> AddUserAccountAsync(IManagementClient client, string username, string password, bool isSuperUser)
        {
            var existingUser = await GetUserAsync(client, username);

            if (existingUser != null && string.Equals(existingUser.Name, username, StringComparison.OrdinalIgnoreCase))
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

            var user = UserInfo.ByPassword(username, password)
                .AddTag(UserTags.Management);

            await client.CreateUserAsync(user);
            await UpdateUserPermissionsAsync(client, username, isSuperUser);

            var userRecord = await client.GetUserAsync(username);
            return userRecord != null
                ? true
                : throw new Exception("Unable to create user");
        }

        private async Task UpdateUserPermissionsAsync(IManagementClient client, string username, bool isSuperUser)
        {
            var user = await GetUserAsync(client, username);

            if (user == null) throw new Exception("User account does not exist");

            var permissionRegex = isSuperUser
                ? ".*"
                : $"{Regex.Escape(username.ToLower())}";

            var configurePermissions = isSuperUser
                ? ".*"
                : "";

            var vHost = await client.GetVhostAsync(SystemVhost);
            await client.CreatePermissionAsync(vHost, new PermissionInfo(username, configurePermissions, permissionRegex, permissionRegex));
        }

        private async Task<User?> GetUserAsync(IManagementClient client, string username)
        {
            try
            {
                return await client.GetUserAsync(username);
            }
            catch (UnexpectedHttpStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private async Task SaveConfigAsync<T>(FileInfo location, T config)
        {
            var options = new JsonSerializerOptions(JsonSerializerOptions.Default)
            {
                WriteIndented = true,
            };

            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(location.FullName, json);
        }
    }
}
