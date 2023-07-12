using Microsoft.Extensions.Configuration;

namespace Abs.CommonCore.Platform.Extensions
{
    public static class ConfigExtensions
    {
        public static TConfig Bind<TConfig>(this IConfiguration config, string? key = null)
            where TConfig : class, new()
        {
            var configModel = new TConfig();

            if (!string.IsNullOrWhiteSpace(key))
            {
                config.Bind(key, configModel);
            }
            else
            {
                config.Bind(configModel);
            }

            return configModel;
        }

        public static TConfig BindToModel<TConfig>(this IConfiguration config, TConfig configModel, string? key = null)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                config.Bind(key, configModel);
            }
            else
            {
                config.Bind(configModel);
            }

            return configModel;
        }
    }
}
