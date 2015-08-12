using Rebus.PostgreSql;
using Rebus.Timeout;
using log4net.Config;

namespace Rebus.Tests.Persistence.Timeouts.Factories
{
    public class PostgreSqlServerTimeoutStorageFactory : ITimeoutStorageFactory
    {
        static PostgreSqlServerTimeoutStorageFactory()
        {
            XmlConfigurator.Configure();
        }

        public IStoreTimeouts CreateStore()
        {
            DropTable("timeouts");
            
            return new PostgreSqlTimeoutStorage(ConnectionStrings.PostgreSql, "timeouts")
                .EnsureTableIsCreated();
        }

        void DropTable(string tableName)
        {
            PostgreSqlFixtureBase.DropTable(tableName);
        }

        public void Dispose()
        {
        }

        protected void DeleteRows(string tableName)
        {
            PostgreSqlFixtureBase.DeleteRows(tableName);
        }
    }
}