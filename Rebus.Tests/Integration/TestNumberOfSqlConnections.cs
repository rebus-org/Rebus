using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.SqlServer;
using Rebus.Transport.SqlServer;
using Timer = System.Timers.Timer;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestNumberOfSqlConnections : FixtureBase
    {
        [Test]
        public async Task CountTheConnections()
        {
            var activeConnections = new ConcurrentDictionary<int, object>();

            var bus = Configure.With(new BuiltinHandlerActivator())
                .Transport(t => t.Register(c =>
                {
                    var connectionProvider = new TestConnectionProvider(SqlTestHelper.ConnectionString, activeConnections);
                    var transport = new SqlServerTransport(connectionProvider, "RebusMessages", "bimse");

                    transport.EnsureTableIsCreated();

                    return transport;
                }))
                .Start();

            using (var printTimer = new Timer(1000))
            {
                printTimer.Elapsed += delegate
                {
                    Console.WriteLine("Active connections: {0}", activeConnections.Count);
                };
                printTimer.Start();

                using (bus)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        class TestConnectionProvider : IDbConnectionProvider
        {
            static int _counter;

            readonly ConcurrentDictionary<int, object> _activeConnections;
            readonly IDbConnectionProvider _inner;

            public TestConnectionProvider(string connectionString, ConcurrentDictionary<int, object> activeConnections)
            {
                _activeConnections = activeConnections;
                _inner = new DbConnectionProvider(connectionString);
            }

            public async Task<IDbConnection> GetConnection()
            {
                return new Bimse(await _inner.GetConnection(), Interlocked.Increment(ref _counter), _activeConnections);
            }

            class Bimse : IDbConnection
            {
                readonly IDbConnection _innerConnection;
                readonly ConcurrentDictionary<int, object> _activeConnections;
                readonly int _id;

                public Bimse(IDbConnection innerConnection, int id, ConcurrentDictionary<int, object> activeConnections)
                {
                    _innerConnection = innerConnection;
                    _id = id;
                    _activeConnections = activeConnections;
                    _activeConnections[id] = new object();
                }

                public SqlCommand CreateCommand()
                {
                    return _innerConnection.CreateCommand();
                }

                public IEnumerable<string> GetTableNames()
                {
                    return _innerConnection.GetTableNames();
                }

                public async Task Complete()
                {
                    await _innerConnection.Complete();
                }

                public void Dispose()
                {
                    _innerConnection.Dispose();

                    object o;
                    _activeConnections.TryRemove(_id, out o);
                }
            }
        }
    }
}