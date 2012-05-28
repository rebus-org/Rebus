using Rebus.Configuration.Configurers;

namespace Rebus.Persistence.SqlServer
{
    public static class SqlServerExtensions
    {
        public static void StoreInSqlServer(this SagaConfigurer configurer, string connectionString, string sagaTableName, string sagaIndexTableName)
        {
            configurer.Use(new SqlServerSagaPersister(connectionString, sagaIndexTableName, sagaTableName));
        }

        public static void StoreInSqlServer(this SubscriptionsConfigurer configurer, string connectionString, string subscriptionsTableName)
        {
            configurer.Use(new SqlServerSubscriptionStorage(connectionString, subscriptionsTableName));
        }
    }
}