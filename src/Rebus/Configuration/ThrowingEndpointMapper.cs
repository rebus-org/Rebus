using System;

namespace Rebus.Configuration
{
    /// <summary>
    /// Endpoint mapper that is meant to be used as the default in cases where
    /// an implementation of <see cref="IDetermineDestination"/> is not chosen.
    /// It will throw every time it gets called, and it will do so with a nice
    /// and friendly error message.
    /// </summary>
    public class ThrowingEndpointMapper : IDetermineDestination
    {
        public string GetEndpointFor(Type messageType)
        {
            throw new ConfigurationException(@"
Rebus is currently not configured with an endpoint mapping mechanism. This means that you take
the responsibility of specifying where messages go, which in turn means that YOU MUST SPECIFY A
DESTINATION EACH TIME YOU SEND OR SUBSCRIBE TO SOMETHING.

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
                .DetermineEndpoint(d => d.FromRebusMappingsSection())
                (....)
                .CreateBus()

which allows you to use Rebus-style endpoint mappings in your application's app.config/web.config.

This is an example on how your app.config might look like if you're planning on sending and/or
subscribing to messages from the MyService.Messages assembly owned by the service that has
my_service.inputQueue as its input queue:

    <?xml version=""1.0""?>
    <configuration>
        <configSections>
            <section name=""RebusMappings"" type=""Rebus.Configuration.RebusMappingsSection, Rebus"" />
        </configSections>
  
        <RebusMappings>
            <Endpoints>
                <add Messages=""MyService.Messages"" Endpoint=""my_service.inputQueue""/>
            </Endpoints>
        </RebusMappings>

        <!-- (...) -->
    </configuration>
");
        }
    }
}