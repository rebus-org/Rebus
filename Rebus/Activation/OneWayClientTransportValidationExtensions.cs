using System;
using Rebus.Config;
using Rebus.Transport;
// ReSharper disable InvertIf

namespace Rebus.Activation;

static class OneWayClientTransportValidationExtensions
{
    public static void ValidateOneWayClient(this OptionsConfigurer options)
    {
        options.Decorate(c =>
        {
            // only validate this if a transport has been configured - otherwise, Rebus' normal validation will let the user know :)
            if (c.Has<ITransport>())
            {
                var transport = c.Get<ITransport>();
                var address = transport.Address;

                // if the transport address is not NULL, it has been configured with an input queue!
                if (address != null)
                {
                    throw new InvalidOperationException(
                        $@"
Tried to configure the transport with input queue name '{address}', but it's not possible for a one-way client to have an input queue!

Please either

    use one of the .Transport(t => t.Use***AsOneWayClient(..)) configuration methods when you're using Configure.OneWayClient() to configure Rebus

or

    pass a handler activator by calling Configure.With(activator) to configure Rebus, where activator is either the BuiltinHandlerActivator or a container adapter
");
                }
            }

            return c.Get<Options>();
        });
    }
}