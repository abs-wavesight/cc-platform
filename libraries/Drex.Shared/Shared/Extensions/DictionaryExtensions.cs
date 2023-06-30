using Rebus.Bus;
using Rebus.Messages;

namespace Abs.CommonCore.Drex.Shared.Extensions
{
    public static class DictionaryExtensions
    {
        public static Dictionary<string, object> ToLogScopeData(this Message message)
        {
            var scopeData = new Dictionary<string, object>
            {
                { "message-id", message.GetMessageId() },
                { "message-type", message.GetMessageType() }
            };

            foreach (var (key, value) in message.Headers)
            {
                var keyToUse = key;
                if (keyToUse.StartsWith(Constants.MessageHeaders.Prefix))
                {
                    keyToUse = keyToUse.Substring(Constants.MessageHeaders.Prefix.Length + 1);
                }

                scopeData.Add(keyToUse, value);
            }

            return scopeData;
        }
    }
}
