using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Abs.CommonCore.Platform.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceRegistrar
{
    public static ILoggingBuilder ConfigureLogging(this ILoggingBuilder logging, bool? useFlatLogs = null)
    {
        logging.ClearProviders();

        useFlatLogs ??= ShouldUseFlatLogs();
        if (useFlatLogs.Value)
        {
            logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss:ffffff ";
                options.ColorBehavior = LoggerColorBehavior.Enabled;
            });
        }
        else
        {
            logging.AddJsonConsole(options =>
            {
                options.TimestampFormat = "u";
                options.IncludeScopes = true;
                options.JsonWriterOptions = new JsonWriterOptions
                {
                    Indented = false,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
            });
        }

        return logging;
    }

    private static bool ShouldUseFlatLogs()
    {
        var flatLogsStr = Environment.GetEnvironmentVariable(PlatformConstants.FlatLogsVariableName);
        if (!bool.TryParse(flatLogsStr, out var useFlatLogs))
        {
            useFlatLogs = false;
        }

        return useFlatLogs;
    }
}
