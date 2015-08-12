using System;
using Rebus.Configuration;

namespace Rebus.Tests.Performance.StressMongo.Factories
{
    public interface IMessageQueueFactory
    {
        Tuple<ISendMessages, IReceiveMessages> GetQueue(string inputQueueName);
        void CleanUp();
        void ConfigureOneWayClientMode(RebusTransportConfigurer configurer);
    }
}