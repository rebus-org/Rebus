using Rebus.Config;

namespace Rebus.Persistence.SqlServer
{
    public static class SqlServerConfiguration
    {
        public static IBusConfigurer UseDbSubscriptionStorage(this IBusConfigurer configurer, string connectionString)
        {
            return configurer
                .WithValue(ConfigurationKeys.SubscriptionStorage, "msmq")
                .WithValue(ConfigurationKeys.SubscriptionStorageConnectionString, connectionString);
        } 
    }

    public class ConfigurationKeys
    {
        // decisions
        public const string SubscriptionStorage = "subscription_storage";
        public const string Transport = "transport";

        // configured values
        public const string SubscriptionStorageConnectionString = "subscription_storage_connection_string";
        public const string MsmqInputQueue = "msmq_input_queue";
    }
}