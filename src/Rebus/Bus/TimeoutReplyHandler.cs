using Newtonsoft.Json;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Bus
{
    class TimeoutReplyHandler : IHandleMessages<TimeoutReply>
    {
        readonly IHandleDeferredMessage handleDeferredMessage;
        internal const string TimeoutReplySecretCorrelationId = "rebus.secret.deferred.message.id";

        static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};

        static ILog log;

        static TimeoutReplyHandler()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public TimeoutReplyHandler(IHandleDeferredMessage handleDeferredMessage)
        {
            this.handleDeferredMessage = handleDeferredMessage;
        }

        public void Handle(TimeoutReply message)
        {
            var deferredMessage = Deserialize(message.CustomData);

            log.Info("Received timeout reply - sending deferred message to self.");

            handleDeferredMessage.Dispatch(deferredMessage);
        }

        static object Deserialize(string customData)
        {
            return JsonConvert.DeserializeObject(customData, JsonSerializerSettings);
        }

        public static string Serialize(object message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None, JsonSerializerSettings);
        }
    }
}