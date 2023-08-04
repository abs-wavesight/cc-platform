using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Abs.CommonCore.Contracts.Json.Drex;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Platform.Config;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Logging;
using PasswordGenerator;

namespace Abs.CommonCore.Installer.Actions
{
    public class RabbitConfigurer
    {
        private readonly ILogger _logger;

        public RabbitConfigurer(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RabbitConfigurer>();
        }

        public async Task<RabbitCredentials?> ConfigureRabbitAsync(Uri rabbit, string rabbitUsername, string rabbitPassword, string username, string? password, string? vhost)
        {
            _logger.LogInformation($"Configuring RabbitMQ at '{rabbit}'");
            var client = new ManagementClient(rabbit, rabbitUsername, rabbitPassword);

            return await ConfigureRabbitAsync(client, username, password, vhost);
        }

        public async Task UpdateDrexSiteConfigAsync(FileInfo location, RabbitCredentials credentials)
        {
            _logger.LogInformation($"Updating Drex site config at '{location}'");
            var config = ConfigParser.LoadConfig<DrexSiteConfig>(location.FullName);

            if (config.RemoteBuses.Count == 0) throw new Exception("No connections to modify.");
            if (config.RemoteBuses.Count != 1) throw new Exception("Multiple connections found.");

            config.RemoteBuses[0].Username = credentials.Username;
            config.RemoteBuses[0].Password = credentials.Password;
            config.RemoteBuses[0].Vhost = credentials.Vhost;

            await SaveConfigAsync(location, config);
        }

        public async Task UpdateDrexClientConfigAsync(FileInfo location, RabbitCredentials credentials)
        {
            _logger.LogInformation($"Updating Drex client config at '{location}'");
            var config = ConfigParser.LoadConfig<DrexClientAppConfig>(location.FullName);

            config.ClientCredentials = new ClientCredentials
            {
                Username = credentials.Username,
                Password = credentials.Password,
                Vhost = credentials.Vhost
            };

            await SaveConfigAsync(location, config);
        }

        private async Task<RabbitCredentials?> ConfigureRabbitAsync(IManagementClient client, string username, string? password, string? vhost)
        {
            // Cryptographically secure password generator: https://github.com/prjseal/PasswordGenerator/blob/0beb483fc6bf796bfa9f81db91265d74f90f29dd/PasswordGenerator/Password.cs#L157
            password = string.IsNullOrWhiteSpace(password)
                ? new Password(true, true, true, true, 32)
                    .Next()
                : password;

            vhost = string.IsNullOrWhiteSpace(vhost)
                ? username
                : vhost;

            var isAdded = await AddUserAccountAsync(client, username, password, vhost);

            if (isAdded == false)
            {
                return null;
            }

            Console.WriteLine("User account created.");
            Console.WriteLine($"User:     {username}");
            Console.WriteLine($"Password: {password}");
            Console.WriteLine($"Vhost:    {vhost}");
            Console.WriteLine();

            return new RabbitCredentials
            {
                Username = username,
                Password = password,
                Vhost = vhost
            };
        }

        private async Task<bool> AddUserAccountAsync(IManagementClient client, string username, string password, string vhost)
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

            await client.CreateVhostAsync(vhost);
            var vHost = await client.GetVhostAsync(vhost);

            var user = UserInfo.ByPassword(username, password)
                .AddTag(UserTags.Management)
                .AddTag(UserTags.Policymaker)
                .AddTag(UserTags.Monitoring);

            await client.CreateUserAsync(user);
            await client.CreatePermissionAsync(vHost, new PermissionInfo(user.Name));

            var userRecord = await client.GetUserAsync(username);
            return userRecord != null
                ? true
                : throw new Exception("Unable to create user");
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
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(location.FullName, json);
        }
    }
}
