using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Transport.Msmq
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqBasicSendReceive : BasicSendReceive<MsmqTransportFactory> { }

    [TestFixture, Category(Categories.Msmq)]
    public class MsmqMessageExpiration : MessageExpiration<MsmqTransportFactory> { }

    public class MsmqTransportFactory : ITransportFactory
    {
        readonly List<IDisposable> _disposables = new List<IDisposable>();
        readonly HashSet<string> _queuesToDelete = new HashSet<string>();

        public ITransport Create(string inputQueueAddress)
        {
            var transport = new MsmqTransport(inputQueueAddress);

            _disposables.Add(transport);

            transport.PurgeInputQueue();

            transport.Initialize();

            _queuesToDelete.Add(inputQueueAddress);

            return transport;
        }

        public void CleanUp()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();

            _queuesToDelete.ForEach(MsmqUtil.Delete);
            _queuesToDelete.Clear();
        }
    }
}