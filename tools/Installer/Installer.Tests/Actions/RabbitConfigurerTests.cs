using Abs.CommonCore.Contracts.Json.Drex;
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Installer.Tests.Actions
{
    public class RabbitConfigurerTests
    {
        [Fact]
        public async Task UpdateDrexSiteConfig_GivenCredentials_SiteConfigUpdated()
        {
            var commandExecution = new CommandExecutionService(NullLoggerFactory.Instance);
            var configurer = new RabbitConfigurer(NullLoggerFactory.Instance, commandExecution);
            var credentials = new RabbitCredentials
            {
                Username = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString(),
            };

            var config = new FileInfo(@"Configs/Drex/site-config.json");
            await configurer.UpdateDrexSiteConfigAsync(config, credentials);

            var configText = await File.ReadAllTextAsync(config.FullName);
            var parsedConfig = ConfigParser.LoadConfig<DrexSiteConfig>(config.FullName);

            Assert.NotNull(parsedConfig);
            Assert.Contains(credentials.Username, configText);
            Assert.Contains(credentials.Password, configText);
        }

        [Fact]
        public async Task GenerateCredentials_EnvironmentVariables_Updated()
        {
            var commandExecution = new Mock<ICommandExecutionService>();
            var configurer = new RabbitConfigurer(NullLoggerFactory.Instance, commandExecution.Object);
            var credentials = new RabbitCredentials
            {
                Username = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString(),
            };

            await configurer.UpdateDrexEnvironmentVariablesAsync(credentials);

            commandExecution
                .Verify(x => x.ExecuteCommandAsync("setx", It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }
    }
}
