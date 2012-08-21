using System.Configuration;
using Rebus.Bus;
using Rebus.Configuration;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.RabbitMQ
{
    public static class RabbitMqConfigurationExtensions
    {
         public static void UseRabbitMq(this RebusTransportConfigurer configurer, string connectionString, string inputQueueName, string errorQueue)
         {
             DoIt(configurer, connectionString, inputQueueName, errorQueue);
         }

         public static void UseRabbitMqAndGetInputQueueNameFromAppConfig(this RebusTransportConfigurer configurer, string connectionString)
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

                 DoIt(configurer, connectionString, inputQueueName, errorQueueName);
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

    <rebus inputQueue=""my.service.input.queue"" errorQueue=""my.service.error.queue"" />

Note also, that specifying the input queue name with the 'inputQueue' attribute is optional.

A more full example configuration snippet can be seen here:

{1}
",
                     e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
             }
         }

         static void DoIt(RebusTransportConfigurer configurer, string connectionString, string inputQueueName, string errorQueueName)
        {
            var queue = new RabbitMqMessageQueue(connectionString, inputQueueName, errorQueueName);
            configurer.UseSender(queue);
            configurer.UseReceiver(queue);
            configurer.UseErrorTracker(new ErrorTracker(errorQueueName));
        }
    }
}