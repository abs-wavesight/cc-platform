using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebex;
using Rebex.Net;
using Rebex.Net.Servers;

namespace Abs.CommonCore.SftpService;

[ExcludeFromCodeCoverage]
public class Program
{
    public static Task Main(string[]? args = null)
    {
        return Run(args: args);
    }

    public static async Task Run(
        Action<HostApplicationBuilder>? setupHost = null,
        Action<HostApplicationBuilder>? overrideHost = null,
        string[]? args = null,
        CancellationToken cancellationToken = default)
    {
        var hostBuilder = Host.CreateApplicationBuilder(args);
        setupHost?.Invoke(hostBuilder);
        hostBuilder.Logging.ConfigureLogging(true);

        var serviceProvider = hostBuilder.Services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        await InitializeServerAsync(logger, cancellationToken);

        overrideHost?.Invoke(hostBuilder);

        var host = hostBuilder.Build();
        await host.RunAsync(cancellationToken);
    }

    public static async Task InitializeServerAsync(ILogger logger,
                                                   CancellationToken cancellationToken)
    {
        // Trial key good until 2023-10-23
        Rebex.Licensing.Key = "==AL4kkXuiIOSH0T0cLM+F3qu2bmz5eWz6CyuVjEMlrk2U==";

        logger.LogInformation("Creating file server");
        var server = new FileServer();

        server.Bind(1022, FileServerProtocol.Sftp);

        InitializeSettings(logger, server);
        await AddSshKeyAsync(logger, server);
        await AddUsersAsync(logger, server);
        SubscribeEvents(logger, server);

        server.Start();
    }

    private static void InitializeSettings(ILogger logger, FileServer server)
    {
        server.LogWriter = new TeeLogWriter(new LogWriterAdapter(logger));
        server.Settings.AcceptWindowsPaths = false;
        server.Settings.ShowHiddenItems = false;
    }

    private static async Task AddSshKeyAsync(ILogger logger, FileServer server)
    {
        logger.LogInformation("Adding ssh keys");

        var data = await File.ReadAllBytesAsync(@"C:\ABS\ssh-keys\ssh_host_rsa_key");
        server.Keys.Add(new SshPrivateKey(data));
    }

    private static async Task AddUsersAsync(ILogger logger, FileServer server)
    {
        await Task.Yield();

        logger.LogInformation("Adding file server users");
        server.Users.Add("drex", "P@ssword", @"c:\");
    }

    private static void SubscribeEvents(ILogger logger, FileServer server)
    {
        server.ShellCommand += (sender, args) => logger.LogInformation($"Shell command: {args.Command}");
        server.ErrorOccurred += (_, args) => logger.LogError(args.Error, "Sftp error");
    }
}
