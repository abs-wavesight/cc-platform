using Google.Protobuf;

namespace Abs.CommonCore.Platform.Extensions;

public static class ProtoMessageExtensions
{
    public static string GetTypeName(this IMessage message)
    {
        return $"{message.Descriptor.File.Name}:{message.Descriptor.Name}";
    }
}
