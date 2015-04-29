using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.SqlServer;
using Rebus.Transport.SqlServer;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestNumberOfSqlConnections : FixtureBase
    {
        static int _counter = 0;

        [Test]
        public async Task CountTheConnections()
        {
            var bus = Configure.With(new BuiltinHandlerActivator())
                .Transport(t => t.Register(c =>
                {
                    var transport = new SqlServerTransport(new TestConnectionProvider(SqlTestHelper.ConnectionString), "RebusMessages", "bimse");

                    transport.EnsureTableIsCreated();

                    return transport;
                }))
                .Start();

            Using(bus);

            await Task.Delay(10000);

            Console.WriteLine("Counter: {0}", _counter);
        }

        class TestConnectionProvider : IDbConnectionProvider
        {
            readonly DbConnectionProvider _inner;

            public TestConnectionProvider(string connectionString)
            {
                _inner = new DbConnectionProvider(connectionString);
            }

            public async Task<IDbConnection> GetConnection()
            {
                return new Bimse(await _inner.GetConnection(), Interlocked.Increment(ref _counter));
            }

            class Bimse : IDbConnection
            {
                readonly IDbConnection _inner;
                readonly int _id;

                public Bimse(IDbConnection inner, int id)
                {
                    _inner = inner;
                    _id = id;
                }

                public SqlCommand CreateCommand()
                {
                    return _inner.CreateCommand();
                }

                public IEnumerable<string> GetTableNames()
                {
                    return _inner.GetTableNames();
                }

                public async Task Complete()
                {
                    await _inner.Complete();
                }

                public void Dispose()
                {
                    _inner.Dispose();
                }
            }
        }
    }
}