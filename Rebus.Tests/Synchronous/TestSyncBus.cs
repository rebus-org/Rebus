using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Synchronous
{
    [TestFixture]
    public class TestSyncBus : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "sync-tjek"))
                .Start();
        }

        [Test]
        public void CanDoItSyncWithoutBlocking()
        {
            var messageWasProperlyHandled = new ManualResetEvent(false);
            var bus = _activator.Bus.Advanced.SyncBus;

            _activator.AddHandlerWithBusTemporarilyStopped<string>(async str => messageWasProperlyHandled.Set());

            var thread = new Thread(() =>
            {
                bus.SendLocal("ey det virker");
            });

            // Setting ApartmentState is not supported in Net 5.0 and on Linux/OSX
            if (Environment.Version.Major !>= 5 && !(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)))
            {
                thread.SetApartmentState(ApartmentState.STA);
            }

            thread.Start();

            Assert.That(thread.Join(1000), Is.True, "thread did not finish within timeout");

            messageWasProperlyHandled.WaitOrDie(timeout: TimeSpan.FromSeconds(2));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SendOperationsEnlistInTheSameTransactionContext(bool commitTransaction)
        {
            var receivedMessages = new ConcurrentQueue<string>();
            var bus = _activator.Bus.Advanced.SyncBus;

            _activator.AddHandlerWithBusTemporarilyStopped<string>(async msg => receivedMessages.Enqueue(msg));

            using (var context = new RebusTransactionScope())
            {
                bus.SendLocal("hej med dig min ven");
                bus.SendLocal("her er endnu en besked");

                if (commitTransaction)
                {
                    context.Complete();
                }
            }

            Thread.Sleep(500);

            if (commitTransaction)
            {
                Assert.That(receivedMessages, Contains.Item("hej med dig min ven"));
                Assert.That(receivedMessages, Contains.Item("her er endnu en besked"));
            }
            else
            {
                Assert.That(receivedMessages.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void CheckException()
        {
            var bus = _activator.Bus.Advanced.SyncBus;

            var argumentException = Assert.Throws<ArgumentException>(() => bus.Send("THIS MESSAGE IS NOT MAPPED TO ANYTHING"));

            Console.WriteLine(argumentException);
        }
    }
}