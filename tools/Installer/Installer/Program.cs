using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Installer.Actions;

namespace Abs.CommonCore.Installer
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var downloadCommand = new Command("download", "Download components for installation");
            downloadCommand.TreatUnmatchedTokensAsErrors = true;

            var registryParam = new Option<FileInfo>("--registry", "Installation registry");
            registryParam.IsRequired = true;
            registryParam.AddAlias("-r");
            downloadCommand.Add(registryParam);

            downloadCommand.SetHandler(async (registry) =>
            {
                var downloader = new ComponentDownloader(registry);
                await downloader.ExecuteAsync();
            }, registryParam);

            var root = new RootCommand("Installer for the Common Core platform");
            root.TreatUnmatchedTokensAsErrors = true;
            root.Add(downloadCommand);

            root.SetHandler(async (mode) =>
            {

            });

            return await root.InvokeAsync(args);
        }
    }
}