using System;

namespace Rebus.Configuration
{
    public class ThrowingEndpointMapper : IDetermineDestination
    {
        public string GetEndpointFor(Type messageType)
        {
            throw new ConfigurationException(@"
You have configured Rebus to be able to SEND messages without configuring an endpoint mapping
mechanism. This means that you take the responsibility of specifying where messages go, which
in turn means that YOU MUST SPECIFY A DESTINATION EACH TIME YOU SEND OR SUBSCRIBE TO SOMETHING.

To resolve this, please either configure an endpoint mapper, or resort only to the

    bus.Send(destination, message)

and

    bus.Subscribe<TMessage>(destination)

overloads on the bus.

I encourage you, however, to configure endpoint mappings by putting all messages owned by each
service in an assembly that serves this purpose only. Hence, endpoint mappings can be configured
by mapping each message type's assembly to an endpoint.

You can achieve this by configuring the bus like so:

    var bus = Configure.With(someContainerAdapter)
                .DetermineEndpoint(d => d.FromNServiceBusConfiguration())
                (....)
                .CreateBus()

which allows you to use NServiceBus-style endpoint mappings in your application's app.config.

This is an example on how your app.config might look like if you're planning on sending and/or
subscribing to messages from the MyService.Messages assembly owned by the service that has
my_service.inputQueue as its input queue:

    <?xml version=""1.0""?>
    <configuration>
      <configSections>
        <section name=""UnicastBusConfig"" type=""NServiceBus.Config.UnicastBusConfig, NServiceBus.Core""/>
      </configSections>
  
      <UnicastBusConfig>
        <MessageEndpointMappings>
          <add Messages=""MyService.Messages"" Endpoint=""my_service.inputQueue""/>
        </MessageEndpointMappings>
      </UnicastBusConfig>
    </configuration>
");
        }
    }
}