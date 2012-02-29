using System.Configuration;
using Rebus.Configuration;
using Rebus.Configuration.Configurers;
using Rebus.Transports.Encrypted;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.Transports.Msmq
{
    public static class MsmqConfigurationExtension
    {
        public static void UseEncryptedMsmq(this TransportConfigurer configurer, string inputQueue, string ivBase64, string keyBase64)
        {
            DoItEncrypted(configurer, inputQueue, ivBase64, keyBase64);
        }

        public static void UseEncryptedMsmqAndGetConfigurationFromAppConfig(this TransportConfigurer configurer)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                var inputQueueName = section.InputQueue;

                if (string.IsNullOrEmpty(inputQueueName))
                {
                    throw new ConfigurationErrorsException("Could not get input queue name from Rebus configuration section. Did you forget the InputQueue attribute?");
                }

                var rijndael = section.RijndaelSection;

                if (rijndael == null)
                {
                    throw new ConfigurationErrorsException("Could not find encryption settings in Rebus configuration section. Did you forget the Rijndael element?");
                }

                if (string.IsNullOrEmpty(rijndael.Iv))
                {
                    throw new ConfigurationErrorsException("Could not find initialization vector settings in Rijndael element - did you forget the Iv attribute?");
                }
                
                if (string.IsNullOrEmpty(rijndael.Key))
                {
                    throw new ConfigurationErrorsException("Could not find key settings in Rijndael element - did you forget the Key attribute?");
                }

                DoItEncrypted(configurer, inputQueueName, rijndael.Iv, rijndael.Key);
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
        <section name=""Rebus"" type=""Rebus.Configuration.RebusConfigurationSection, Rebus"" />
        <!-- other stuff in here as well -->
    </configSections>

-and then you need a <Rebus> element some place further down the app.config/web.config,
like so:

    <Rebus InputQueue=""my.service.input.queue"">
        <Rijndael Iv=""initialization vector here"" Key=""key here""/>
    </Rebus>

Note also, that specifying the input queue name with the InputQueue attribute is optional.

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

                var inputQueueName = section.InputQueue;

                if (string.IsNullOrEmpty(inputQueueName))
                {
                    throw new ConfigurationErrorsException("Could not get input queue name from Rebus configuration section. Did you forget the InputQueue attribute?");
                } 

                DoIt(configurer, inputQueueName);
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

        static void DoIt(TransportConfigurer configurer, string inputQueueName)
        {
            if (string.IsNullOrEmpty(inputQueueName))
            {
                throw new ConfigurationErrorsException("You need to specify an input queue.");
            }

            var msmqMessageQueue = new MsmqMessageQueue(inputQueueName);

            configurer.UseSender(msmqMessageQueue);
            configurer.UseReceiver(msmqMessageQueue);
        }

        static void DoItEncrypted(TransportConfigurer configurer, string inputQueueName, string iv, string key)
        {
            if (string.IsNullOrEmpty(inputQueueName))
            {
                throw new ConfigurationErrorsException("You need to specify an input queue.");
            }

            var msmqMessageQueue = new MsmqMessageQueue(inputQueueName);
            var encryptionFilter = new EncryptionFilter(msmqMessageQueue, msmqMessageQueue, iv, key);

            configurer.UseSender(encryptionFilter);
            configurer.UseReceiver(encryptionFilter);
        }
    }
}