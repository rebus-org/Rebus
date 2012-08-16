using System;
using System.Configuration;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Configuration.Configurers;
using Rebus.Shared;
using Rebus.Transports.Encrypted;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.Transports.Msmq
{
    public static class MsmqConfigurationExtension
    {
        public static void UseEncryptedMsmq(this TransportConfigurer configurer, string inputQueue, string errorQueue, string keyBase64)
        {
            DoItEncrypted(configurer, inputQueue, keyBase64, errorQueue);
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
                    throw new ConfigurationErrorsException(string.Format(@"Could not find encryption settings in Rebus configuration section. Did you forget the 'rijndael' element?

{0}", GenerateRijndaelHelp()));
                }
                
                if (string.IsNullOrEmpty(rijndael.Key))
                {
                    throw new ConfigurationErrorsException(string.Format(@"Could not find key settings in Rijndael element - did you forget the 'key' attribute?

{0}", GenerateRijndaelHelp()));
                }

                DoItEncrypted(configurer, inputQueueName, rijndael.Key, errorQueueName);
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
        <rijndael iv=""base64 encoded initialization vector"" key=""base64 encoded key""/>
    </rebus>

Note also, that specifying the input queue name with the 'inputQueue' attribute is optional.

A more full example configuration snippet can be seen here:

{1}
",
                    e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
            }            
        }

        static string GenerateRijndaelHelp()
        {
            return
                string.Format(
                    @"To help you get started, here's a valid rijndael element, including a fresh and newly generated
key - and yes, I promise that it has been generated just this moment :) you can try running your app
again if you don't believe me.

    <rijndael key=""{0}""/>

The key has been generated with the biggest valid size, so it should be pretty secure.
",
                    RijndaelEncryptionTransportDecorator.GenerateKeyBase64());
        }

        /// <summary>
        /// Specifies that you want to use MSMQ to both send and receive messages. The input
        /// queue will be automatically created if it doesn't exist.
        /// </summary>
        public static void UseMsmq(this TransportConfigurer configurer, string inputQueue, string errorQueue)
        {
            DoIt(configurer, inputQueue, errorQueue);
        }

        public static void UseMsmqInOneWayClientMode(this TransportConfigurer configurer)
        {
            var msmqMessageQueue = MsmqMessageQueue.Sender();

            configurer.UseSender(msmqMessageQueue);
            var gag = new OneWayClientGag();
            configurer.UseReceiver(gag);
            configurer.UseErrorTracker(gag);
        }

        public class OneWayClientGag : IReceiveMessages, IErrorTracker
        {
            public ReceivedTransportMessage ReceiveMessage()
            {
                throw new NotImplementedException();
            }

            public string InputQueue { get; private set; }
            public string InputQueueAddress { get; private set; }
            public void StopTracking(string id)
            {
                throw new NotImplementedException();
            }

            public bool MessageHasFailedMaximumNumberOfTimes(string id)
            {
                throw new NotImplementedException();
            }

            public string GetErrorText(string id)
            {
                throw new NotImplementedException();
            }

            public void TrackDeliveryFail(string id, Exception exception)
            {
                throw new NotImplementedException();
            }

            public string ErrorQueueAddress { get; private set; }
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

            var msmqMessageQueue = new MsmqMessageQueue(inputQueueName);

            var errorQueuePath = MsmqUtil.GetPath(errorQueueName);

            MsmqUtil.EnsureMessageQueueExists(errorQueuePath);
            MsmqUtil.EnsureMessageQueueIsTransactional(errorQueuePath);

            configurer.UseSender(msmqMessageQueue);
            configurer.UseReceiver(msmqMessageQueue);
            configurer.UseErrorTracker(new ErrorTracker(errorQueueName));
        }

        static void DoItEncrypted(TransportConfigurer configurer, string inputQueueName, string key, string errorQueueName)
        {
            if (string.IsNullOrEmpty(inputQueueName))
            {
                throw new ConfigurationErrorsException("You need to specify an input queue.");
            }

            var msmqMessageQueue = new MsmqMessageQueue(inputQueueName);
            var encryptionFilter = new RijndaelEncryptionTransportDecorator(msmqMessageQueue, msmqMessageQueue, key);

            var errorQueuePath = MsmqUtil.GetPath(errorQueueName);

            MsmqUtil.EnsureMessageQueueExists(errorQueuePath);
            MsmqUtil.EnsureMessageQueueIsTransactional(errorQueuePath);

            configurer.UseSender(encryptionFilter);
            configurer.UseReceiver(encryptionFilter);
            configurer.UseErrorTracker(new ErrorTracker(errorQueueName));
        }
    }
}