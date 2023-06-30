using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using PasswordGenerator;

namespace Abs.CommonCore.Drex.RabbitMqConfigure
{
    public class RabbitManager
    {
        private readonly IManagementClient _localClient;
        private readonly IManagementClient _remoteClient;

        public RabbitManager(string local, string remote, string username, string password)
        {
            _localClient = new ManagementClient(new Uri(local), username, password);
            _remoteClient = new ManagementClient(new Uri(remote), username, password);
        }

        public async Task SetupRabbitMqAsync(string clientName, bool isGlobalUser)
        {
            if (isGlobalUser == false)
            {
                await AddVHostAsync(clientName);
            }

            await AddUserAccountAsync(clientName, isGlobalUser);
        }

        public async Task AddVHostAsync(string clientName)
        {
            await AddVHostAsync(clientName, _localClient);
            await AddVHostAsync(clientName, _remoteClient);
        }

        public async Task AddUserAccountAsync(string clientName, bool isGlobalUser)
        {
            // Cryptographically secure password generator: https://github.com/prjseal/PasswordGenerator/blob/0beb483fc6bf796bfa9f81db91265d74f90f29dd/PasswordGenerator/Password.cs#L157
            var generator = new Password(true, true, true, true, 32);
            
            var password = generator.Next();
            var username = clientName;

            await AddUserAccountAsync(clientName, username, password, isGlobalUser, _localClient);
            await AddUserAccountAsync(clientName, username, password, isGlobalUser, _remoteClient);

            var vHostName = isGlobalUser
                ? "/"
                : clientName;

            Console.WriteLine("User account created. This must be kept safe as it is not saved anywhere.");
            Console.WriteLine($"User:     {username}");
            Console.WriteLine($"Password: {password}");
            Console.WriteLine($"Vhost:    {vHostName}");
            Console.WriteLine();
        }

        private async Task AddVHostAsync(string clientName, IManagementClient client)
        {
            await client.CreateVhostAsync(clientName);
            var vHost = await client.GetVhostAsync(clientName);

            if (vHost == null)
            {
                throw new Exception("Unable to create RabbitMQ virtual host");
            }
        }

        private async Task AddUserAccountAsync(string clientName, string username, string password, bool isGlobalUser, IManagementClient client)
        {
            var vHostName = isGlobalUser
                ? "/"
                : clientName;

            var vHost = await client.GetVhostAsync(vHostName);

            var user = UserInfo.ByPassword(username, password)
                .AddTag(UserTags.Management)
                .AddTag(UserTags.Policymaker)
                .AddTag(UserTags.Monitoring);

            await client.CreateUserAsync(user);
            await client.CreatePermissionAsync(vHost, new PermissionInfo(user.Name));

            var userRecord = await client.GetUserAsync(username);
            if (userRecord == null)
            {
                throw new Exception("Unable to create user");
            }
        }
    }
}
