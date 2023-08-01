using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Platform.Config;
using Microsoft.Extensions.Logging.Abstractions;

namespace Installer.Tests.Actions
{
    public class ReleaseBodyBuilderTests : TestsBase
    {
        [Fact]
        public async Task ReleaseBody_Config_ParametersFound()
        {
            await TestReleaseBodyAsync(@"Configs/ParameterDownloaderConfig.json", new Dictionary<string, string>());
        }

        [Fact]
        public async Task ReleaseBody_Parameters_ParametersFound()
        {
            var parameters = new Dictionary<string, string>()
            {
                {"$Test_Param1", "TestValue1"},
                {"$Test_Param2", "TestValue2"},
                {"$Test_Param3", "TestValue3"},
            };

            await TestReleaseBodyAsync(null, parameters);
        }

        [Fact]
        public async Task ReleaseBody_All_ParametersFound()
        {
            var parameters = new Dictionary<string, string>()
            {
                {"$Some_Other_Test_Param1", "OtherTestValue1"},
                {"$Some_Other_Test_Param2", "OtherTestValue2"},
                {"$Some_Other_Test_Param3", "OtherTestValue3"},
            };

            await TestReleaseBodyAsync(@"Configs/ParameterDownloaderConfig.json", parameters);
        }

        private async Task TestReleaseBodyAsync(string? configPath, Dictionary<string, string>? parameters)
        {
            var builder = new ReleaseBodyBuilder(NullLoggerFactory.Instance);

            var configFile = string.IsNullOrWhiteSpace(configPath)
                ? null
                : new FileInfo(configPath);
            var outputFile = Path.GetTempFileName();

            try
            {
                await builder.BuildReleaseBodyAsync(configFile, parameters, new FileInfo(outputFile));

                Assert.True(File.Exists(outputFile));

                var outputText = await File.ReadAllTextAsync(outputFile);

                if (string.IsNullOrWhiteSpace(configPath) == false)
                {
                    var config = ConfigParser.LoadConfig<InstallerComponentDownloaderConfig>(configFile.FullName);
                    foreach (var param in config.Parameters)
                    {
                        Assert.Contains(param.Value, outputText);
                    }
                }

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        Assert.Contains(param.Value, outputText);
                    }
                }
            }
            finally
            {
                File.Delete(outputFile);
            }
        }
    }
}
