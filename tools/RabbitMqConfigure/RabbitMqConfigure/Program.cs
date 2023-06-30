using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace Abs.CommonCore.Drex.RabbitMqConfigure
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var userParam = new Option<string>("--user", "Username for the connection");
            userParam.IsRequired = true;
            userParam.AddAlias("-u");

            var passwordParam = new Option<string>("--password", "Password for the connection");
            passwordParam.IsRequired = true;
            passwordParam.AddAlias("-p");

            var clientParam = new Option<string>("--client", "Name of the client to initialize");
            clientParam.IsRequired = true;
            clientParam.AddAlias("-c");

            var localAddressParam = new Option<string>("--local", "Address to use for local connection");
            localAddressParam.IsRequired = true;
            localAddressParam.AddAlias("-l");

            var remoteAddressParam = new Option<string>("--remote", "Address to use for remote connection");
            remoteAddressParam.IsRequired = true;
            remoteAddressParam.AddAlias("-r");

            var isGlobalParam = new Option<bool>("--global", "Indicates the account is for a global user. Default is false.");
            isGlobalParam.AddAlias("-g");

            var root = new RootCommand("Configuration tool for RabbitMQ connections");
            root.TreatUnmatchedTokensAsErrors = true;
            root.Add(userParam);
            root.Add(passwordParam);
            root.Add(clientParam);
            root.Add(localAddressParam);
            root.Add(remoteAddressParam);
            root.Add(isGlobalParam);

            root.SetHandler(async (user, password, client, local, remote, isGlobal) =>
            {
                Console.WriteLine($"Client: {client}");
                Console.WriteLine($"Local: {local}");
                Console.WriteLine($"Remote: {remote}");
                Console.WriteLine();

                try
                {
                    var manager = new RabbitManager(local, remote, user, password);
                    await manager.SetupRabbitMqAsync(client, isGlobal);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to configure instances");
                    Console.WriteLine(ex);
                    Console.ResetColor();
                }
            }, userParam, passwordParam, clientParam, localAddressParam, remoteAddressParam, isGlobalParam);

            return await root.InvokeAsync(args);
        }
    }
}