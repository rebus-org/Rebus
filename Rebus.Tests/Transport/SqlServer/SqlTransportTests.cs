using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;
using Rebus.Transport.SqlServer;

namespace Rebus.Tests.Transport.SqlServer
{
    [TestFixture, Category(Categories.SqlServer)]
    public class SqlServerTransportBasicSendReceive : BasicSendReceive<SqlTransportFactory> { }

    [TestFixture, Category(Categories.SqlServer)]
    public class SqlServerTransportMessageExpiration : MessageExpiration<SqlTransportFactory> { }

    public class SqlTransportFactory : ITransportFactory
    {
        readonly HashSet<string> _tablesToDrop = new HashSet<string>();
        readonly List<IDisposable> _disposables = new List<IDisposable>();

        public ITransport CreateOneWayClient()
        {
            var tableName = ("RebusMessages_" + TestConfig.Suffix).TrimEnd('_');

            _tablesToDrop.Add(tableName);

            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, consoleLoggerFactory);
            var transport = new SqlServerTransport(connectionProvider, tableName, null, consoleLoggerFactory);

            _disposables.Add(transport);

            transport.EnsureTableIsCreated();
            transport.Initialize();

            return transport;
        }

        public ITransport Create(string inputQueueAddress)
        {
            var tableName = ("RebusMessages_" + TestConfig.Suffix).TrimEnd('_');

            _tablesToDrop.Add(tableName);

            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, consoleLoggerFactory);
            var transport = new SqlServerTransport(connectionProvider, tableName, inputQueueAddress, consoleLoggerFactory);
            
            _disposables.Add(transport);
            
            transport.EnsureTableIsCreated();
            transport.Initialize();
            
            return transport;
        }

        public void CleanUp()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();

            _tablesToDrop.ForEach(SqlTestHelper.DropTable);
            _tablesToDrop.Clear();
        }
    }
}