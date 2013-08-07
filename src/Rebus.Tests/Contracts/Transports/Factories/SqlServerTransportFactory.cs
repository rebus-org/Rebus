using System;
using System.Collections.Generic;
using Rebus.Tests.Persistence;
using Rebus.Transports.Sql;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public class SqlServerTransportFactory : ITransportFactory
    {
        readonly List<IDisposable> disposables = new List<IDisposable>();

        public Tuple<ISendMessages, IReceiveMessages> Create()
        {
            var sender = GetQueue("test.contracts.sender");
            var receiver = GetQueue("test.contracts.receiver");

            return Tuple.Create<ISendMessages, IReceiveMessages>(sender, receiver);
        }

        public void CleanUp()
        {
            disposables.ForEach(d => d.Dispose());
        }

        public IReceiveMessages CreateReceiver(string queueName)
        {
            var receiver = GetQueue(queueName);

            return receiver;
        }

        IDuplexTransport GetQueue(string inputQueueName)
        {
            var queue = new SqlServerMessageQueue(SqlServerFixtureBase.ConnectionString, "messages2", inputQueueName)
                .EnsureTableIsCreated()
                .PurgeInputQueue();
            
            disposables.Add(queue);
            
            return queue;
        }
    }
}