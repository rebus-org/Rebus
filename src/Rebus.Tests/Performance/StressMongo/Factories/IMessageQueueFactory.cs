using System;

namespace Rebus.Tests.Performance.StressMongo.Factories
{
    public interface IMessageQueueFactory
    {
        Tuple<ISendMessages, IReceiveMessages> GetQueue(string inputQueueName);
        void CleanUp();
    }
}