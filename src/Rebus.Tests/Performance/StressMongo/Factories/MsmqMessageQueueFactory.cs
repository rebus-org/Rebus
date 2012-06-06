using System;
using System.Collections.Generic;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Performance.StressMongo.Factories
{
    public class MsmqMessageQueueFactory : IMessageQueueFactory
    {
        readonly List<IDisposable> disposables = new List<IDisposable>();

        public Tuple<ISendMessages, IReceiveMessages> GetQueue(string inputQueueName)
        {
            var messageQueue = new MsmqMessageQueue(inputQueueName, "error").PurgeInputQueue();
            MsmqUtil.PurgeQueue("error");
            disposables.Add(messageQueue);
            return new Tuple<ISendMessages, IReceiveMessages>(messageQueue, messageQueue);
        }

        public void CleanUp()
        {
            disposables.ForEach(d => d.Dispose());
        }
    }
}