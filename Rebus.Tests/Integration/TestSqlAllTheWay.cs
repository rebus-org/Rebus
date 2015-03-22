using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Extensions;
using Rebus.Transport.SqlServer;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(Categories.SqlServer)]
    public class TestSqlAllTheWay : FixtureBase
    {
        BuiltinHandlerActivator _handlerActivator;
        IBus _bus;
        static readonly string ConnectionString = SqlTestHelper.ConnectionString;

        protected override void SetUp()
        {
            SqlTestHelper.DropTable("RebusMessages");

            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Transport(x => x.UseSqlServer(ConnectionString, "RebusMessages", "test.input"))
                .Sagas(x => x.StoreInSqlServer(ConnectionString, "Sagas", "SagaIndex"))
                .Options(x =>
                {
                    x.SetNumberOfWorkers(1);
                    x.SetMaxParallelism(1);
                })
                .Start();

            Using(_bus);
        }

        [Test]
        public async Task SendAndReceiveOneSingleMessage()
        {
            var gotTheMessage = new ManualResetEvent(false);

            _handlerActivator.Handle<string>(async message =>
            {
                Console.WriteLine("w00000t! Got message: {0}", message);
                gotTheMessage.Set();
            });

            await _bus.SendLocal("hej med dig min ven!");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(10));
        }
    }
}