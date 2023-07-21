using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Installer.Tests.Actions
{
    public class ComponentInstallerTests
    {
        [Fact]
        public void InvalidInstallerConfig_ThrowsException()
        {
            Assert.Throws<ConfigException>(() => Initialize(@"Configs/Invalid_RegistryConfig.json", @"Configs/Invalid_InstallerConfig.json"));
        }

        [Fact]
        public async Task InvalidInstallerConfigValues_ThrowsException()
        {
            var initializer = Initialize(@"Configs/RegistryConfig.json", @"Configs/InvalidComponent_InstallerConfig.json");
            await Assert.ThrowsAsync<Exception>(() => initializer.Installer.ExecuteAsync());
        }

        [Fact]
        public async Task ValidConfig_InstallAction()
        {
            var initializer = Initialize(@"Configs/RegistryConfig.json");
            await initializer.Installer.ExecuteAsync(new[] { "RabbitMq" });

            initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync("docker", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ParameterizedConfig_InstallAction()
        {
            var paramKey = "$SOME_INSTALL_PARAM";
            var paramValue = "Replacement";

            var parameters = new Dictionary<string, string>() { { paramKey, paramValue } };
            var initializer = Initialize(@"Configs/ParameterizedRegistryConfig.json", parameters: parameters);
            await initializer.Installer.ExecuteAsync(new[] { "RabbitMq" });

            initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync(paramValue, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private (Mock<ICommandExecutionService> CommandExecute, ComponentInstaller Installer) Initialize(string registryFile, string? installerFile = null, Dictionary<string, string>? parameters = null)
        {
            var commandExecute = new Mock<ICommandExecutionService>();

            var registryFileInfo = new FileInfo(registryFile);
            var installerFileInfo = string.IsNullOrWhiteSpace(installerFile) == false
                ? new FileInfo(installerFile)
                : null;

            parameters ??= new Dictionary<string, string>();
            var downloader = new ComponentInstaller(NullLoggerFactory.Instance, commandExecute.Object, registryFileInfo, installerFileInfo, parameters);
            return (commandExecute, downloader);
        }
    }
}
