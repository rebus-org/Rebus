using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Transport.SqlServer;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestBugWhenSendingMessagesInParallel : FixtureBase
    {
        [Test]
        public void ShouldNotFailWhenSendingPublishingMessageToManySubscribersWithSqlTransport()
        {
            var inMemorySubscriptionStorage = new InMemorySubscriptionStorage();
            inMemorySubscriptionStorage.RegisterSubscriber("TradeFinalized", "a").Wait();
            inMemorySubscriptionStorage.RegisterSubscriber("TradeFinalized", "b").Wait();
            inMemorySubscriptionStorage.RegisterSubscriber("TradeFinalized", "c").Wait();
            inMemorySubscriptionStorage.RegisterSubscriber("TradeFinalized", "d").Wait();
            inMemorySubscriptionStorage.RegisterSubscriber("TradeFinalized", "e").Wait();
            inMemorySubscriptionStorage.RegisterSubscriber("TradeFinalized", "f").Wait();
            inMemorySubscriptionStorage.RegisterSubscriber("TradeFinalized", "g").Wait();
            inMemorySubscriptionStorage.RegisterSubscriber("TradeFinalized", "h").Wait();
            inMemorySubscriptionStorage.RegisterSubscriber("TradeFinalized", "i").Wait();


            var bus = Configure.With(new BuiltinHandlerActivator())
                .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                .Transport(t => t.UseSqlServer("server=.;database=demo;trusted_connection=true", "messages", "trading"))
                .Subscriptions(s => s.Register(c => inMemorySubscriptionStorage))
                .Start();

            bus.Publish("TradeFinalized", new TheMessage()).Wait();
        }

        public class TheMessage
        {
            
        }
    }
}