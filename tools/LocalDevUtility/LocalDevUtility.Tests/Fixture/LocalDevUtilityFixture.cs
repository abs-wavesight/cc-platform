using System.Reflection;
using Abs.CommonCore.LocalDevUtility.Commands.Configure;
using Abs.CommonCore.LocalDevUtility.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Abs.CommonCore.LocalDevUtility.Tests.Fixture;

public class LocalDevUtilityFixture
{
    public Mock<IPowerShellAdapter> MockPowerShellAdapter { get; set; }

    public PowerShellAdapter RealPowerShellAdapter { get; set; }
    public readonly ILogger Logger;

    public List<string> ActualPowerShellCommands { get; set; } = new();

    public LocalDevUtilityFixture(ITestOutputHelper testOutput)
    {
        MockPowerShellAdapter = new Mock<IPowerShellAdapter>();
        MockPowerShellAdapter
            .Setup(a => a.RunPowerShellCommand(It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .Callback<string, TimeSpan?>((commandItem, _) => ActualPowerShellCommands.Add(commandItem.Replace("\"\"", "\"")));

        TestLogger.Default.SetTestOutput(testOutput);
        Logger = TestLogger.Default;
        RealPowerShellAdapter = new PowerShellAdapter();
    }

    public async Task ExecuteApplication(string input)
    {
        var inputArray = input.Split(" ");
        await Program.Run(inputArray, TestLogger.Default, MockPowerShellAdapter.Object);
    }

    public static AppConfig GetValidTestConfig()
    {
        var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var repoRootPath = Path.GetFullPath(Path.Combine(executingPath, "../../../../../.."));

        var dummyCertPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-certs");
        var dummyLocalKeys = Path.Combine(dummyCertPath, "local-keys");
        var dummyLocalCerts = Path.Combine(dummyCertPath, "local-certs");
        Directory.CreateDirectory(dummyLocalKeys);
        Directory.CreateDirectory(dummyLocalCerts);

        var testsCertPath = Path.Combine(executingPath, "dummy-certs");
        var testsLocalKeys = Path.Combine(testsCertPath, "local-keys");
        var testsLocalCerts = Path.Combine(testsCertPath, "local-certs");

        CopyFile(testsLocalKeys, dummyLocalKeys, "rabbitmq.key");
        CopyFile(testsLocalCerts, dummyLocalCerts, "rabbitmq.cer");
        CopyFile(testsLocalCerts, dummyLocalCerts, "rabbitmq.pem");

        var dummySftpRootPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-sftp-root");
        Directory.CreateDirectory(dummySftpRootPath);

        var dummyFdzRootPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-fdz-root");
        Directory.CreateDirectory(dummyFdzRootPath);

        var dummyDrexPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-cc-drex-repo");
        var dummydrexServicePath = Path.Combine(dummyDrexPath, "service");
        Directory.CreateDirectory(dummydrexServicePath);

        var dummydrexClientPath = Path.Combine(dummyDrexPath, "client");
        Directory.CreateDirectory(dummydrexClientPath);

        var dummydrexDemoPath = Path.Combine(dummyDrexPath, "demo");
        Directory.CreateDirectory(dummydrexDemoPath);

        CopyFile(repoRootPath, dummydrexServicePath, "nuget.config");
        CopyFile(repoRootPath, dummydrexClientPath, "nuget.config");
        CopyFile(repoRootPath, dummydrexDemoPath, "nuget.config");

        var dummyDiscoRepoPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-cc-disco-repo");
        Directory.CreateDirectory(dummyDiscoRepoPath);

        var dummySiemensAdapterPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-cc-siemens-adapter-repo");
        Directory.CreateDirectory(dummySiemensAdapterPath);

        var dummyKdiAdapterPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-cc-kdi-adapter-repo");
        Directory.CreateDirectory(dummyKdiAdapterPath);

        var dummyVoyageManagerAdapterPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-cc-voyage-manager-adapter-repo");
        Directory.CreateDirectory(dummyVoyageManagerAdapterPath);

        var dummySchedulerPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-cc-scheduler-repo");
        Directory.CreateDirectory(dummySchedulerPath);

        return new AppConfig
        {
            CommonCorePlatformRepositoryPath = repoRootPath,
            CommonCoreDrexRepositoryPath = dummyDrexPath,
            CommonCoreDiscoRepositoryPath = dummyDiscoRepoPath,
            CommonCoreSiemensAdapterRepositoryPath = dummySiemensAdapterPath,
            CommonCoreKdiAdapterRepositoryPath = dummyKdiAdapterPath,
            VoyageManagerRepositoryPath = dummyVoyageManagerAdapterPath,
            CommonCoreSchedulerRepositoryPath = dummySchedulerPath,
            ContainerWindowsVersion = "2019",
            CertificatePath = dummyCertPath,
            SshKeysPath = dummyCertPath,
            SftpRootPath = dummySftpRootPath,
            FdzRootPath = dummyFdzRootPath
        };
    }

    public static async Task SetUpValidTestConfig()
    {
        await SetUpConfig(GetValidTestConfig());
    }

    public static async Task SetUpConfig(AppConfig? config)
    {
        ConfigureCommand.ValidateConfig(config).Should().HaveCount(0);
        await ConfigureCommand.SaveConfig(config);
    }

    private static void CopyFile(string sourcePath, string destinationPath, string name)
    {
        sourcePath = Path.Combine(sourcePath, name);
        destinationPath = Path.Combine(destinationPath, name);

        File.Copy(sourcePath, destinationPath, true);
    }
}
