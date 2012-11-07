using Newtonsoft.Json;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    /// Special internal message handler that handles timeout replies from the timeout manager
    /// </summary>
    class TimeoutReplyHandler : IHandleMessages<TimeoutReply>
    {
        internal const string TimeoutReplySecretCorrelationId = "rebus.secret.deferred.message.id";

        static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};

        static ILog log;

        static TimeoutReplyHandler()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly IHandleDeferredMessage handleDeferredMessage;

        public TimeoutReplyHandler(IHandleDeferredMessage handleDeferredMessage)
        {
            this.handleDeferredMessage = handleDeferredMessage;
        }

        public void Handle(TimeoutReply message)
        {
            if (message.CorrelationId != TimeoutReplySecretCorrelationId)
                return;

            var deferredMessage = Deserialize(message.CustomData);

            log.Info("Received timeout reply - sending deferred message to self.");

            handleDeferredMessage.Dispatch(deferredMessage, message.SagaId);
        }

        object Deserialize(string customData)
        {
            return JsonConvert.DeserializeObject(customData, JsonSerializerSettings);
        }

        public static string Serialize(object message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None, JsonSerializerSettings);
        }
    }
}