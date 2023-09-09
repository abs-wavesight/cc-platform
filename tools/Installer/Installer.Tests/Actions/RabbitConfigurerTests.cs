using Abs.CommonCore.Contracts.Json.Drex;
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Platform.Config;

namespace Installer.Tests.Actions;

public class RabbitConfigurerTests
{
    [Fact]
    public async Task UpdateDrexSiteConfig_GivenCredentials_SiteConfigUpdated()
    {
        var credentials = new RabbitCredentials
        {
            Username = Guid.NewGuid().ToString(),
            Password = Guid.NewGuid().ToString(),
        };

        var config = new FileInfo(@"Configs/Drex/site-config.json");
        await RabbitConfigurer.UpdateDrexSiteConfigAsync(config, credentials);

        var configText = await File.ReadAllTextAsync(config.FullName);
        var parsedConfig = ConfigParser.LoadConfig<DrexSiteConfig>(config.FullName);

        Assert.NotNull(parsedConfig);
        Assert.Contains(credentials.Username, configText);
        Assert.Contains(credentials.Password, configText);
    }

    [Fact]
    public async Task GenerateCredentials_UpdateFile_FileUpdated()
    {
        var credentials = new RabbitCredentials
        {
            Username = Guid.NewGuid().ToString(),
            Password = Guid.NewGuid().ToString(),
        };

        var credentialsFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(credentialsFile, "Username: $USERNAME\r\nPassword: $PASSWORD");

        await RabbitConfigurer.UpdateCredentialsFileAsync(credentials, new FileInfo(credentialsFile));

        var text = await File.ReadAllTextAsync(credentialsFile);

        Assert.Contains(credentials.Username, text);
        Assert.Contains(credentials.Password, text);
    }
}
