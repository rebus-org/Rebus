using log4net.Config;
using Rebus.PostgreSql;

namespace Rebus.Tests.Persistence.Sagas.Factories
{
    public class PostgreSqlServerSagaPersisterFactory : ISagaPersisterFactory
    {
        static PostgreSqlServerSagaPersisterFactory()
        {
            XmlConfigurator.Configure();
        }

        public IStoreSagaData CreatePersister()
        {
            const string sagaTableName = "test_sagas";
            const string sagaIndexTableName = "test_saga_index";
            var sqlServerSagaPersister = new PostgreSqlSagaPersister(ConnectionStrings.PostgreSql, sagaIndexTableName, sagaTableName);
            sqlServerSagaPersister.EnsureTablesAreCreated();
            DeleteRows(sagaTableName);
            DeleteRows(sagaIndexTableName);
            return sqlServerSagaPersister;
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