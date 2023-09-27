using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Rebex;
using LogLevel = Rebex.LogLevel;

namespace Abs.CommonCore.SftpService;

[ExcludeFromCodeCoverage]
public class LogWriterAdapter : LogWriterBase
{
    private readonly ILogger _logger;
    private const string MessageTemplate = "{type} {id} {area} {message}";
    private const string MessageTemplateWithData = "{type} {id} {area} {message} {data}";

    public LogWriterAdapter(ILogger logger)
    {
        _logger = logger;
    }

    private void Write(LogLevel level, Type objectType, int objectId, string area, string message, ArraySegment<byte>? data)
    {
        var template = (data == null)
            ? MessageTemplate
            : MessageTemplateWithData;

        if (level <= LogLevel.Verbose)
        {
            _logger.LogTrace(template, objectType, objectId, area, message, data);
        }
        else if (level <= LogLevel.Debug)
        {
            _logger.LogDebug(template, objectType, objectId, area, message, data);
        }
        else if (level <= LogLevel.Info)
        {
            _logger.LogInformation(template, objectType, objectId, area, message, data);
        }
        else if (level <= LogLevel.Error)
        {
            _logger.LogError(template, objectType, objectId, area, message, data);
        }
    }

    public override void Write(LogLevel level, Type objectType, int objectId, string area, string message)
    {
        Write(level, objectType, objectId, area, message, null);
    }

    public override void Write(LogLevel level, Type objectType, int objectId, string area, string message, byte[] buffer, int offset, int length)
    {
        Write(level, objectType, objectId, area, message, new ArraySegment<byte>(buffer, offset, length));
    }
}
