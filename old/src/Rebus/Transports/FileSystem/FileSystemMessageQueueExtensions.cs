using System.Configuration;
using Rebus.Bus;
using Rebus.Configuration;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.Transports.FileSystem
{
    /// <summary>
    /// Extensions for making it easy to cofigure the file system transport
    /// </summary>
    public static class FileSystemMessageQueueExtensions
    {
        /// <summary>
        /// Configures Rebus to run in one-way client mode, which means that the bus is capable only of sending messages.
        /// </summary>
        public static void UseTheFileSystemInOneWayClientMode(this RebusTransportConfigurer configurer, string baseDirectory)
        {
            var transport = FileSystemMessageQueue.Sender(baseDirectory);

            configurer.UseSender(transport);
            var gag = new OneWayClientGag();
            configurer.UseReceiver(gag);
            configurer.UseErrorTracker(gag);
        }

        /// <summary>
        /// Specifies that you want to use the file system to both send and receive messages. The input queue will be automatically created if it doesn't exist.
        /// Please note that the file system trans
        /// </summary>
        public static void UseTheFileSystem(this RebusTransportConfigurer configurer, string baseDirectory, string inputQueueName, string errorQueueName)
        {
            DoIt(configurer, baseDirectory, inputQueueName, errorQueueName);
        }

        /// <summary>
        /// Specifies that you want to use the file system to both send and receive messages. The input queue name will be deduced from the Rebus configuration 
        /// section in the application configuration file. The input queue will be automatically created if it doesn't exist.
        /// </summary>
        public static void UseTheFileSystemAndGetInputQueueNameFromAppConfig(this RebusTransportConfigurer configurer, string baseDirectory)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                section.VerifyPresenceOfInputQueueConfig();
                section.VerifyPresenceOfErrorQueueConfig();

                var inputQueueName = section.InputQueue;
                var errorQueueName = section.ErrorQueue;

                DoIt(configurer, baseDirectory, inputQueueName, errorQueueName);
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

    <rebus inputQueue=""my_service_input_queue"" errorQueue=""my_service_error_queue"" />

Note also, that specifying the input queue name with the 'inputQueue' attribute is optional.

A more full example configuration snippet can be seen here:

{1}
",
                    e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
            }
        }

        static void DoIt(RebusTransportConfigurer configurer, string baseDirectory, string inputQueueName, string errorQueueName)
        {
            var transport = new FileSystemMessageQueue(baseDirectory, inputQueueName);

            configurer.UseSender(transport);
            configurer.UseReceiver(transport);
            configurer.UseErrorTracker(new ErrorTracker(errorQueueName));
        }
    }
}