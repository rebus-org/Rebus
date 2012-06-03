using System.Configuration;
using Rebus.Configuration;
using Rebus.Configuration.Configurers;
using Rebus.Transports.Encrypted;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.Transports.Msmq
{
    public static class MsmqConfigurationExtension
    {
        public static void UseEncryptedMsmq(this TransportConfigurer configurer, string inputQueue, string errorQueue, string ivBase64, string keyBase64)
        {
            DoItEncrypted(configurer, inputQueue, ivBase64, keyBase64, errorQueue);
        }

        public static void UseEncryptedMsmqAndGetConfigurationFromAppConfig(this TransportConfigurer configurer)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                var inputQueueName = section.InputQueue;

                if (string.IsNullOrEmpty(inputQueueName))
                {
                    throw new ConfigurationErrorsException("Could not get input queue name from Rebus configuration section. Did you forget the 'inputQueue' attribute?");
                }

                var errorQueueName = section.ErrorQueue;

                if (string.IsNullOrEmpty(errorQueueName))
                {
                    throw new ConfigurationErrorsException("Could not get error queue name from Rebus configuration section. Did you forget the 'errorQueue' attribute?");
                }

                var rijndael = section.RijndaelSection;

                if (rijndael == null)
                {
                    throw new ConfigurationErrorsException("Could not find encryption settings in Rebus configuration section. Did you forget the 'rijndael' element?");
                }

                if (string.IsNullOrEmpty(rijndael.Iv))
                {
                    throw new ConfigurationErrorsException("Could not find initialization vector settings in Rijndael element - did you forget the 'iv' attribute?");
                }
                
                if (string.IsNullOrEmpty(rijndael.Key))
                {
                    throw new ConfigurationErrorsException("Could not find key settings in Rijndael element - did you forget the 'key' attribute?");
                }

                DoItEncrypted(configurer, inputQueueName, rijndael.Iv, rijndael.Key, errorQueueName);
            }
            catch (ConfigurationErrorsException e)
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

    <rebus InputQueue=""my.service.input.queue"">
        <rijndael iv=""initialization vector here"" key=""key here""/>
    </rebus>

Note also, that specifying the input queue name with the 'inputQueue' attribute is optional.

A more full example configuration snippet can be seen here:

{1}
",
                    e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
            }            
        }

        /// <summary>
        /// Specifies that you want to use MSMQ to both send and receive messages. The input
        /// queue will be automatically created if it doesn't exist.
        /// </summary>
        public static void UseMsmq(this TransportConfigurer configurer, string inputQueue, string errorQueue)
        {
            DoIt(configurer, inputQueue, errorQueue);
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

                var inputQueueName = section.InputQueue;

                if (string.IsNullOrEmpty(inputQueueName))
                {
                    throw new ConfigurationErrorsException("Could not get input queue name from Rebus configuration section. Did you forget the 'inputQueue' attribute?");
                } 

                var errorQueueName = section.ErrorQueue;

                if (string.IsNullOrEmpty(errorQueueName))
                {
                    throw new ConfigurationErrorsException("Could not get input queue name from Rebus configuration section. Did you forget the 'errorQueue' attribute?");
                } 

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

        static void DoIt(TransportConfigurer configurer, string inputQueueName, string errorQueueName)
        {
            if (string.IsNullOrEmpty(inputQueueName))
            {
                throw new ConfigurationErrorsException("You need to specify an input queue.");
            }

            var msmqMessageQueue = new MsmqMessageQueue(inputQueueName, errorQueueName);

            configurer.UseSender(msmqMessageQueue);
            configurer.UseReceiver(msmqMessageQueue);
        }

        static void DoItEncrypted(TransportConfigurer configurer, string inputQueueName, string iv, string key, string errorQueueName)
        {
            if (string.IsNullOrEmpty(inputQueueName))
            {
                throw new ConfigurationErrorsException("You need to specify an input queue.");
            }

            var msmqMessageQueue = new MsmqMessageQueue(inputQueueName, errorQueueName);
            var encryptionFilter = new RijndaelEncryptionTransportDecorator(msmqMessageQueue, msmqMessageQueue, iv, key);

            configurer.UseSender(encryptionFilter);
            configurer.UseReceiver(encryptionFilter);
        }
    }
}