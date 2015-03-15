using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Timeouts;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture]
    public class BasicStoreAndRetrieveOperations : BasicStoreAndRetrieveOperations<SqlServerTimeoutManagerFactory>
    {
    }

    public class SqlServerTimeoutManagerFactory : ITimeoutManagerFactory
    {
        const string TableName = "RebusTimeouts";

        readonly List<IDisposable> _disposables = new List<IDisposable>();

        public ITimeoutManager Create()
        {
            var timeoutManager = new SqlServerTimeoutManager(new DbConnectionProvider(SqlTestHelper.ConnectionString), TableName);

            timeoutManager.EnsureTableIsCreated();

            _disposables.Add(timeoutManager);

            return timeoutManager;
        }

        public void Cleanup()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();

            SqlTestHelper.DropTable(TableName);
        }
    }
}