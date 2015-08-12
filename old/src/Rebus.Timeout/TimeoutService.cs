using System;
using System.Collections;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using System.Linq;

namespace Rebus.Timeout
{
    public class TimeoutService : IActivateHandlers
    {
        static ILog log;

        static TimeoutService()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public const string DefaultInputQueueName = "rebus.timeout";
        public const string DefaultErrorQueueName = "rebus.timeout.error";

        RebusBus rebusBus;

        static readonly Type[] IgnoredMessageTypes =
            new[]
                {
                    typeof (object),
                    typeof (IRebusControlMessage),
                    typeof (TimeoutRequest)
                };

        public TimeoutService(IStoreTimeouts storeTimeouts)
        {
            var msmqMessageQueue = new MsmqMessageQueue(DefaultInputQueueName);
            Initialize(storeTimeouts, msmqMessageQueue, msmqMessageQueue, DefaultErrorQueueName);
        }

        public TimeoutService(IStoreTimeouts storeTimeouts, string inputQueueName, string errorQueueName)
        {
            var msmqMessageQueue = new MsmqMessageQueue(inputQueueName);
            Initialize(storeTimeouts, msmqMessageQueue, msmqMessageQueue, errorQueueName);
        }

        public TimeoutService(IStoreTimeouts storeTimeouts, ISendMessages sendMessages, IReceiveMessages receiveMessages)
        {
            Initialize(storeTimeouts, sendMessages, receiveMessages, DefaultErrorQueueName);
        }

        void Initialize(IStoreTimeouts storeTimeouts, ISendMessages sendMessages, IReceiveMessages receiveMessages, string errorQueueName)
        {
            var errorQueuePath = MsmqUtil.GetPath(errorQueueName);
            MsmqUtil.EnsureMessageQueueExists(errorQueuePath);
            MsmqUtil.EnsureMessageQueueIsTransactional(errorQueuePath);

            rebusBus = new RebusBus(this, sendMessages, receiveMessages, null, null, null,
                                    new JsonMessageSerializer(),
                                    new TrivialPipelineInspector(),
                                    new ErrorTracker(errorQueueName),
                                    storeTimeouts, new ConfigureAdditionalBehavior());
        }

        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            if (IgnoredMessageTypes.Contains(typeof(T)))
            {
                return new IHandleMessages<T>[0];
            }

            throw new InvalidOperationException(string.Format("Someone took the chance and sent a message of type {0} to me.", typeof(T)));
        }

        public void Release(IEnumerable handlerInstances)
        {
        }

        public void Start()
        {
            log.Info("Starting bus");
            rebusBus.Start(1);
        }

        public void Stop()
        {
            log.Info("Disposing bus");
            rebusBus.Dispose();
        }
    }
}