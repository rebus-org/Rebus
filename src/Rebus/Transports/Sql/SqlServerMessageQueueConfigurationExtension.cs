using System.Configuration;
using Rebus.Bus;
using Rebus.Configuration;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.Transports.Sql
{
    /// <summary>
    /// Configuration extensions that allow for configuring Rebus to use <see cref="SqlServerMessageQueue"/> as a message transport
    /// </summary>
    public static class SqlServerMessageQueueConfigurationExtension
    {
        /// <summary>
        /// Default name of SQL Server table that will be used to store Rebus messages
        /// </summary>
        public const string DefaultMessagesTableName = "RebusMessages";

        /// <summary>
        /// Specifies that you want to use Sql Server to both send and receive messages. The input
        /// queue will be automatically created if it doesn't exist.
        /// </summary>
        public static SqlServerMessageQueueOptions UseSqlServer(this RebusTransportConfigurer configurer, string connectionStringOrConnectionStringName, string inputQueue, string errorQueue)
        {
            return configurer.UseSqlServer(connectionStringOrConnectionStringName, DefaultMessagesTableName, inputQueue, errorQueue);
        }

        /// <summary>
        /// Specifies that you want to use Sql Server to both send and receive messages. The input
        /// queue will be automatically created if it doesn't exist.
        /// </summary>
        public static SqlServerMessageQueueOptions UseSqlServer(this RebusTransportConfigurer configurer, string connectionStringOrConnectionStringName, string MessageTableName, string inputQueue, string errorQueue)
        {
            return DoIt(connectionStringOrConnectionStringName, configurer, MessageTableName, inputQueue, errorQueue);
        }

        /// <summary>
        /// Configures Rebus to run in one-way client mode, which means that the bus is capable only of sending messages.
        /// </summary>
        public static SqlServerMessageQueueOptions UseSqlServerInOneWayClientMode(this RebusTransportConfigurer configurer, string connectionStringOrConnectionStringName)
        {
            var connectionStringToUse = Rebus.Shared.ConnectionStringUtil.GetConnectionStringToUse(connectionStringOrConnectionStringName);
            var sqlServerMessageQueue = SqlServerMessageQueue.Sender(connectionStringToUse, DefaultMessagesTableName);

            configurer.UseSender(sqlServerMessageQueue);
            var gag = new OneWayClientGag();
            configurer.UseReceiver(gag);
            configurer.UseErrorTracker(gag);

            return new SqlServerMessageQueueOptions(sqlServerMessageQueue);
        }

        /// <summary>
        /// Configures Rebus to run in one-way client mode, which means that the bus is capable only of sending messages.
        /// </summary>
        public static SqlServerMessageQueueOptions UseSqlServerInOneWayClientMode(this RebusTransportConfigurer configurer, string connectionStringOrConnectionStringName, string MessageTableName)
        {
            var connectionStringToUse = Rebus.Shared.ConnectionStringUtil.GetConnectionStringToUse(connectionStringOrConnectionStringName);
            var sqlServerMessageQueue = SqlServerMessageQueue.Sender(connectionStringToUse, MessageTableName);

            configurer.UseSender(sqlServerMessageQueue);
            var gag = new OneWayClientGag();
            configurer.UseReceiver(gag);
            configurer.UseErrorTracker(gag);

            return new SqlServerMessageQueueOptions(sqlServerMessageQueue);
        }

        /// <summary>
        /// Specifies that you want to use Sql Server to both send and receive messages. The input
        /// queue name will be deduced from the Rebus configuration section in the application
        /// configuration file. The input queue will be automatically created if it doesn't exist.
        /// </summary>
        public static SqlServerMessageQueueOptions UseSqlServerAndGetInputQueueNameFromAppConfig(this RebusTransportConfigurer configurer, string connectionStringOrConnectionStringName, string MessageTableName = null)
        {
            try
            {
                string messageTableNameToUse = string.IsNullOrEmpty(MessageTableName) ? DefaultMessagesTableName : MessageTableName;

                var section = RebusConfigurationSection.LookItUp();

                section.VerifyPresenceOfInputQueueConfig();
                section.VerifyPresenceOfErrorQueueConfig();                

                var inputQueueName = section.InputQueue;
                var errorQueueName = section.ErrorQueue;

                return DoIt(connectionStringOrConnectionStringName, configurer, messageTableNameToUse, inputQueueName, errorQueueName);
            }
            catch(ConfigurationErrorsException e)
            {
                throw new ConfigurationException(
                    @"
An error occurred when trying to parse out the configuration of the RebusConfigurationSection:

{0}

-

For this way of configuring input queue to work, you need to supply a correct configuration
section declaration in the <configSections> element of your app.config/web.config - like so:

    <configSections>
        <section name=""rebus"" type=""Rebus.Configuration.RebusConfigurationSection, Rebus"" />
        <!-- other stuff in here as well -->
    </configSections>

-and then you need a <rebus> element some place further down the app.config/web.config,
like so:

    <rebus inputQueue=""my.service.input.queue"" errorQueue=""my.service.error.queue"" />

Note also, that specifying the input queue name with the 'inputQueue' attribute is optional.

A more full example configuration snippet can be seen here:

{1}
",
                    e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
            }
        }

        static SqlServerMessageQueueOptions DoIt(string connectionStringOrConnectionStringName, RebusTransportConfigurer configurer, string messageTableName, string inputQueueName, string errorQueueName)
        {
            var connectionStringToUse = Rebus.Shared.ConnectionStringUtil.GetConnectionStringToUse(connectionStringOrConnectionStringName);

            if (string.IsNullOrEmpty(inputQueueName))
            {
                throw new ConfigurationErrorsException("You need to specify an input queue.");
            }

            if(string.IsNullOrEmpty(messageTableName))
            {
                throw new ConfigurationException("You need to specify message table name.");
            }

            var sqlServerMessageQueue = new SqlServerMessageQueue(connectionStringToUse, messageTableName, inputQueueName);

            configurer.UseSender(sqlServerMessageQueue);
            configurer.UseReceiver(sqlServerMessageQueue);
            configurer.UseErrorTracker(new ErrorTracker(errorQueueName));

            return new SqlServerMessageQueueOptions(sqlServerMessageQueue);
        }
    }
}