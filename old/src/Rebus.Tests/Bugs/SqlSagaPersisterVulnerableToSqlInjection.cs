using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Tests.Persistence;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class SqlSagaPersisterVulnerableToSqlInjection : SqlServerFixtureBase
    {
        const string InputQueueName = "sql.inject.input";
        const string ErrorQueueName = "error";
        BuiltinContainerAdapter adapter;

        protected override void DoSetUp()
        {
            adapter = TrackDisposable(new BuiltinContainerAdapter());

            DropTables();

            adapter.Register(typeof (MySaga));

            Configure.With(adapter)
                .Logging(l => l.ColoredConsole(minLevel:LogLevel.Warn))
                .Transport(t => t.UseMsmq(InputQueueName, ErrorQueueName))
                .Sagas(s => s.StoreInSqlServer(ConnectionString, "sagas", "saga_index")
                    .EnsureTablesAreCreated())
                .CreateBus()
                .Start();
        }

        protected override void DoTearDown()
        {
            CleanUpTrackedDisposables();

            MsmqUtil.Delete(InputQueueName);
            MsmqUtil.Delete(ErrorQueueName);

            //DropTables();
        }

        static void DropTables()
        {
            DropTable("sagas");
            DropTable("saga_index");
            DropTable("important_data");
        }

        [Test]
        public void CannotInjectSql()
        {
            ExecuteCommand("create table important_data (data nvarchar(255))");
            ExecuteCommand("insert into important_data ([data]) values ('this is the most important string in the world')");

            var countBefore = ExecuteScalar("select count(*) from important_data");

            // let's see if we can inject something nasty into this one at {3}:
            //    insert into [saga_index] ([saga_type], [key], [value], [saga_id]) values ('{1}', '{2}', '{3}', '{4}')
            //
            //     ', '7b5b7456-ed03-4f02-bbfa-dbcfc2dbdfb0')
            const string maliciousSql = @"bim', '5ED81F77-D256-494B-A798-42FD950428F3');  -- finish off first line with some nonsense

-- execute evilness
delete from [important_data];   

-- continue where we left off so that the appendix makes up a valid statement again
insert into [saga_index] ([saga_type], [key], [value], [saga_id]) values ('bim', 'bom', 'bommelom";
            adapter.Bus.SendLocal(new MyMessage { CorrelationProperty = maliciousSql });

            Thread.Sleep(2.Seconds());

            var countAfter = ExecuteScalar("select count(*) from important_data");

            Assert.That(countBefore, Is.EqualTo(1));
            Assert.That(countAfter, Is.EqualTo(1), "Did NOT expect malicious SQL to have been executed - bummer dude!");
        }

        class MySaga : Saga<MySagaData>, IAmInitiatedBy<MyMessage>
        {
            public override void ConfigureHowToFindSaga()
            {
                Incoming<MyMessage>(m => m.CorrelationProperty).CorrelatesWith(d => d.CorrelationProperty);
            }

            public void Handle(MyMessage message)
            {
                Data.CorrelationProperty = message.CorrelationProperty;
                Data.MessageCounter++;
            }
        }

        class MyMessage
        {
            public string CorrelationProperty { get; set; }
        }

        class MySagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string CorrelationProperty { get; set; }
            public int MessageCounter { get; set; }
        }
    }
}