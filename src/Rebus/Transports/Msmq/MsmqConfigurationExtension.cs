using System.Configuration;
using Rebus.Configuration;
using Rebus.Configuration.Configurers;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.Transports.Msmq
{
    public static class MsmqConfigurationExtension
    {
        /// <summary>
        /// Specifies that you want to use MSMQ to both send and receive messages. The input
        /// queue will be automatically created if it doesn't exist.
        /// </summary>
        public static void UseMsmq(this TransportConfigurer configurer, string inputQueue)
        {
            DoIt(configurer, inputQueue);
        }

        /// <summary>
        /// Specifies that you want to use MSMQ to both send and receive messages. The input
        /// queue name will be deduced from the Rebus configuration section in the application
        /// configuration file. The input queue will be automatically created if it doesn't exist.
        /// </summary>
        public static void UseMsmqAndGetInputQueueNameFromAppConfig(this TransportConfigurer configurer)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                DoIt(configurer, section.InputQueue);
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
        <section name=""Rebus"" type=""Rebus.Configuration.RebusConfigurationSection, Rebus"" />
        <!-- other stuff in here as well -->
    </configSections>

-and then you need a <Rebus> element some place further down the app.config/web.config,
like so:

    <Rebus InputQueue=""my.service.input.queue"" />

Note also, that specifying the input queue name with the InputQueue attribute is optional.

A more full example configuration snippet can be seen here:

{1}
",
                    e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
            }
        }

        static void DoIt(TransportConfigurer configurer, string inputQueue)
        {
            var msmqMessageQueue = new MsmqMessageQueue(inputQueue);

            configurer.UseSender(msmqMessageQueue);
            configurer.UseReceiver(msmqMessageQueue);
        }
    }
}