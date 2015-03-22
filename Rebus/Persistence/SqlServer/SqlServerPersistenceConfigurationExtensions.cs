using Rebus.Config;
using Rebus.Sagas;

namespace Rebus.Persistence.SqlServer
{
    public static class SqlServerPersistenceConfigurationExtensions
    {
        public static void StoreInSqlServer(this StandardConfigurer<ISagaStorage> configurer, 
            string connectionStringOrConnectionStringName, string dataTableName, string indexTableName,
            bool automaticallyCreateTables = true)
        {
            configurer.Register(c =>
            {
                var sagaStorage = new SqlServerSagaStorage(
                    new DbConnectionProvider(connectionStringOrConnectionStringName),
                    dataTableName, indexTableName);

                if (automaticallyCreateTables)
                {
                    sagaStorage.EnsureTablesAreCreated();
                }

                return sagaStorage;
            });
        }
    }
}