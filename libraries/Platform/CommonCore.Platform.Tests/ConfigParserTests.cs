using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Tests.Models;
using Xunit;

namespace Abs.CommonCore.Platform.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void ValidConfig_FileParsed()
        {
            var config = ConfigParser.LoadConfig<TestConfig>(@"Configs/ValidConfig.json");

            Assert.Equal(123, config.IntValue);
            Assert.Equal("Test string", config.StringValue);
        }

        [Fact]
        public void ValidConfig_Preprocess_FileParsed()
        {
            var config = ConfigParser.LoadConfig<TestConfig>(@"Configs/ValidConfig.json",
                (c, v) =>
                {
                    return v
                        .Replace(c.IntValue.ToString(), (c.IntValue + 1).ToString())
                        .Replace(c.StringValue!, c.StringValue + "-x");
                });

            Assert.Equal(124, config.IntValue);
            Assert.Equal("Test string-x", config.StringValue);
        }

        [Fact]
        public void InvalidConfig_FileParsed()
        {
            Assert.Throws<Exception>(() =>
            {
                try
                {
                    return ConfigParser.LoadConfig<TestConfig>(@"Configs/InvalidConfig.json");
                }
                catch (Exception ex)
                {
                    // The actual thrown exception is internal to System.Text.Json
                    throw new Exception("Error", ex);
                }
            });
        }
    }
}
