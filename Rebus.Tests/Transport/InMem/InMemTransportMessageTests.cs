using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Transport.InMem
{
    [TestFixture]
    public class InMemTransportMessageTests
    {
        [Test]
        public void DeterminesAgeByRebusTime()
        {
            try
            {
                RebusTime.SetFactory(() => new DateTimeOffset(2000, 01, 01, 00, 00, 00, TimeSpan.Zero));
            
                var transportMessage = new TransportMessage(new Dictionary<string, string>(), new byte[] {1, 2, 3});
                var inMemTransportMessage = new InMemTransportMessage(transportMessage);
            
                RebusTime.SetFactory(() => new DateTimeOffset(2000, 01, 02, 00, 00, 00, TimeSpan.Zero));

                Assert.That(inMemTransportMessage.Age, Is.EqualTo(TimeSpan.FromDays(1)));
            }
            finally
            {
                RebusTime.Reset();
            }
        }
    }
}
