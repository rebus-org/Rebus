using System.Configuration;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Shared;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// Configuration extensions that allow for configuring Rebus to use <see cref="MsmqMessageQueue"/> as a message transport
    /// </summary>
    public static class MsmqConfigurationExtension
    {
        /// <summary>
        /// Specifies that you want to use MSMQ to both send and receive messages. The input
        /// queue will be automatically created if it doesn't exist.
        /// </summary>
        public static void UseMsmq(this RebusTransportConfigurer configurer, string inputQueue, string errorQueue)
        {
            DoIt(configurer, inputQueue, errorQueue);
        }

        /// <summary>
        /// Configures Rebus to run in one-way client mode, which means that the bus is capable only of sending messages.
        /// </summary>
        public static void UseMsmqInOneWayClientMode(this RebusTransportConfigurer configurer)
        {
            var msmqMessageQueue = MsmqMessageQueue.Sender();

            configurer.UseSender(msmqMessageQueue);
            var gag = new OneWayClientGag();
            configurer.UseReceiver(gag);
            configurer.UseErrorTracker(gag);
        }

        /// <summary>
        /// Specifies that you want to use MSMQ to both send and receive messages. The input
        /// queue name will be deduced from the Rebus configuration section in the application
        /// configuration file. The input queue will be automatically created if it doesn't exist.
        /// </summary>
        public static void UseMsmqAndGetInputQueueNameFromAppConfig(this RebusTransportConfigurer configurer)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                section.VerifyPresenceOfInputQueueConfig();
                section.VerifyPresenceOfErrorQueueConfig();

                var inputQueueName = section.InputQueue;
                var errorQueueName = section.ErrorQueue;

                DoIt(configurer, inputQueueName, errorQueueName);
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

        static void DoIt(RebusTransportConfigurer configurer, string inputQueueName, string errorQueueName)
        {
            if (string.IsNullOrEmpty(inputQueueName))
            {
                throw new ConfigurationErrorsException("You need to specify an input queue.");
            }

            var msmqMessageQueue = new MsmqMessageQueue(inputQueueName);

            // since these operations only make sense to perform on a local queue, we'll skip it if the error queue is remote
            // (read http://blogs.msdn.com/b/johnbreakwell/archive/2008/07/31/checking-if-msmq-queues-exist-is-hard-work-so-should-you-bother.aspx 
            // for more info...)
            if (MsmqUtil.IsLocal(errorQueueName))
            {
                var errorQueuePath = MsmqUtil.GetPath(errorQueueName);

                MsmqUtil.EnsureMessageQueueExists(errorQueuePath);
                MsmqUtil.EnsureMessageQueueIsTransactional(errorQueuePath);
            }

            configurer.UseSender(msmqMessageQueue);
            configurer.UseReceiver(msmqMessageQueue);
            configurer.UseErrorTracker(new ErrorTracker(errorQueueName));
        }
    }
}