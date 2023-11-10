using Abs.CommonCore.Installer;
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Installer.Tests.Actions;

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
    public async Task ValidConfig_CopyAction()
    {
        Directory.CreateDirectory(@"c:\abs\installer\CopyTest");
        await File.WriteAllTextAsync(@"c:\abs\installer\CopyTest\x", "Test content");

        var initializer = Initialize(@"Configs/InstallTest_RegistryConfig.json");
        await initializer.Installer.ExecuteAsync(new[] { "CopyTest" });

        initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync("copy", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task ValidConfig_InstallAction()
    {
        Directory.CreateDirectory(@"c:\abs\installer\InstallTest");
        await File.WriteAllTextAsync(@"c:\abs\installer\InstallTest\x.tar", "Test content");

        var initializer = Initialize(@"Configs/InstallTest_RegistryConfig.json");
        await initializer.Installer.ExecuteAsync(new[] { "InstallTest" });

        var dockerPath = DockerPath.GetDockerPath();
        initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync(dockerPath,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task ValidConfig_ExecuteAction()
    {
        var initializer = Initialize(@"Configs/InstallTest_RegistryConfig.json");
        await initializer.Installer.ExecuteAsync(new[] { "ExecuteTest" });

        initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync("x-action", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task ValidConfig_ExecuteImmediateAction()
    {
        var initializer = Initialize(@"Configs/InstallTest_RegistryConfig.json");

        var commandCalls = new List<string>();
        initializer.CommandExecute
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, string, string, bool>((c, _, _, _) => commandCalls.Add(c));

        await initializer.Installer.ExecuteAsync(new[] { "ExecuteImmediateTest" });

        initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(2));
        Assert.True(commandCalls.Count == 2 && commandCalls[0] == "first" && commandCalls[1] == "last");
    }

    [Fact]
    public async Task ValidConfig_UpdatePathAction()
    {
        var initializer = Initialize(@"Configs/InstallTest_RegistryConfig.json");
        await initializer.Installer.ExecuteAsync(new[] { "UpdatePathTest" });

        initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync("setx", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task ValidConfig_ReplaceParametersAction()
    {
        var sourceValue = "$PARAM value";
        var configPath = @"c:\\config\\params.json";
        Directory.CreateDirectory(@"c:\\config");
        await File.WriteAllTextAsync(configPath, sourceValue);

        var parameters = new Dictionary<string, string>
        {
            { "$PARAM", "SomeValue"}
        };

        var expected = sourceValue.Replace(parameters.First().Key, parameters.First().Value, StringComparison.OrdinalIgnoreCase);

        var initializer = Initialize(@"Configs/InstallTest_RegistryConfig.json", parameters: parameters);
        await initializer.Installer.ExecuteAsync(new[] { "ReplaceParametersTest" });

        var newText = await File.ReadAllTextAsync(configPath);
        Assert.Equal(expected, newText);
    }

    [Fact]
    public async Task ValidConfig_RunDockerComposeAction()
    {
        Directory.CreateDirectory("C:\\config\\test-app1");
        Directory.CreateDirectory("C:\\config\\test-app2");

        await File.WriteAllTextAsync(@"c:\\config\\docker-compose.root.yml", "Invalid content");
        await File.WriteAllTextAsync(@"c:\\config\\test-app1\\docker-compose.test-app1.yml", "Invalid content");
        await File.WriteAllTextAsync(@"c:\\config\\test-app2\\docker-compose.test-app2.yml", "Invalid content");

        var initializer = Initialize(@"Configs/InstallTest_RegistryConfig.json");
        initializer.Installer.WaitForDockerContainersHealthy = false;
        await initializer.Installer.ExecuteAsync(new[] { "RunDockerComposeTest" });

        var args = "";
        var dockerComposePath = DockerPath.GetDockerComposePath();
        initializer.CommandExecute.Setup(x => x.ExecuteCommandAsync(dockerComposePath, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, string, string, bool>((_, a, _, _) => args = a);

        initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync(dockerComposePath, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        Assert.Contains(args, "docker-compose.root.yml");
        Assert.Contains(args, "docker-compose.test-app1.yml");
        Assert.Contains(args, "docker-compose.test-app2.yml");
    }

    [Fact]
    public async Task ParameterizedConfig_InstallAction()
    {
        var paramKey = "$SOME_INSTALL_PARAM";
        var paramValue = "Replacement";

        var parameters = new Dictionary<string, string>() { { paramKey, paramValue } };
        var initializer = Initialize(@"Configs/ParameterizedRegistryConfig.json", parameters: parameters);
        await initializer.Installer.ExecuteAsync(new[] { "RabbitMq" });

        initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync(paramValue, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task ValidConfig_RealFileInstalled()
    {
        var loggerFactory = NullLoggerFactory.Instance;
        var commandExecution = new CommandExecutionService(loggerFactory);
        var serviceManager = new Mock<IServiceManager>();
        var registry = new FileInfo(@"Configs/InstallTest_RegistryConfig.json");
        var config = new FileInfo(@"Configs/InstallerConfig.json");
        var parameters = new Dictionary<string, string>();

        var rootPath = @"c:\abs\installer\RabbitMq";
        Directory.CreateDirectory(rootPath);

        var sourcePath = Path.Combine(rootPath, "install_file");
        var destinationPath = Path.Combine(rootPath, "install_file_2");

        await File.WriteAllTextAsync(sourcePath, "This is some test content");

        var installer = new ComponentInstaller(loggerFactory, commandExecution, serviceManager.Object, registry, config, parameters, false);
        await installer.ExecuteAsync();

        Assert.True(File.Exists(destinationPath));
    }

    private static (Mock<ICommandExecutionService> CommandExecute, ComponentInstaller Installer) Initialize(string registryFile, string? installerFile = null, Dictionary<string, string>? parameters = null)
    {
        var commandExecute = new Mock<ICommandExecutionService>();
        var serviceManager = new Mock<IServiceManager>();

        var registryFileInfo = new FileInfo(registryFile);
        var installerFileInfo = string.IsNullOrWhiteSpace(installerFile) == false
            ? new FileInfo(installerFile)
            : null;

        parameters ??= new Dictionary<string, string>();
        var downloader = new ComponentInstaller(NullLoggerFactory.Instance, commandExecute.Object, serviceManager.Object, registryFileInfo, installerFileInfo, parameters, false);
        return (commandExecute, downloader);
    }
}
