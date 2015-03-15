using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture]
    public class BasicSubscriptionOperations : BasicSubscriptionOperations<SqlServerSubscriptionStorageFactory>
    {
    }

    public class SqlServerSubscriptionStorageFactory : ISubscriptionStorageFactory
    {
        const string TableName = "RebusSubscriptions";
        readonly List<IDisposable> _disposables = new List<IDisposable>();
        
        public ISubscriptionStorage Create()
        {
            var storage = new SqlServerSubscriptionStorage(new DbConnectionProvider(SqlTestHelper.ConnectionString), TableName, true);

            _disposables.Add(storage);
            
            storage.EnsureTableIsCreated();
            
            return storage;
        }

        public void Cleanup()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();

            SqlTestHelper.DropTable(TableName);
        }
    }
}