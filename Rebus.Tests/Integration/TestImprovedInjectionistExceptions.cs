using System;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Integration
{
    public class TestImprovedInjectionistExceptions : FixtureBase
    {
        /*
The exception used to look like this (i.e. like a ResolutionException with several inner ResolutionExceptions,
finally wrapping the actual InvalidOperationException):

    Rebus.Injection.ResolutionException : Could not resolve Rebus.Bus.IBus with decorator depth 0 - registrations: Rebus.Injection.Injectionist+Handler
      ----> Rebus.Injection.ResolutionException : Could not resolve Rebus.Bus.IBus with decorator depth 1 - registrations: Rebus.Injection.Injectionist+Handler
      ----> Rebus.Injection.ResolutionException : Could not resolve Rebus.Bus.IBus with decorator depth 2 - registrations: Rebus.Injection.Injectionist+Handler
      ----> Rebus.Injection.ResolutionException : Could not resolve Rebus.Bus.IBus with decorator depth 3 - registrations: Rebus.Injection.Injectionist+Handler
      ----> Rebus.Injection.ResolutionException : Could not resolve Rebus.Bus.IBus with decorator depth 4 - registrations: Rebus.Injection.Injectionist+Handler
      ----> System.InvalidOperationException : oh no!! THIS is the actual exception - everything else is noise

One ResolutionException is enough!

    Rebus.Injection.ResolutionException : Could not resolve Rebus.Bus.IBus with decorator depth 4 - registrations: Rebus.Injection.Injectionist+Handler
      ----> System.InvalidOperationException : oh no!! THIS is the actual exception - everything else is noise

        */
        [Fact]
        public void UsedToLookPrettyBad()
        {
            var activator = Using(new BuiltinHandlerActivator());

            try
            {
                Configure.With(activator)
                    .Transport(t => t.UseInMemoryTransportAsOneWayClient(new InMemNetwork()))
                    .Options(o =>
                    {
                        // somewhere deep down, an exception is thrown...
                        o.Decorate<IBus>(c =>
                        {
                            throw new InvalidOperationException(
                                "oh no!! THIS is the actual exception - everything else is noise");
                        });

                        // and it is thrown beneatj all of these bad boys
                        o.Decorate(c => c.Get<IBus>());
                        o.Decorate(c => c.Get<IBus>());
                        o.Decorate(c => c.Get<IBus>());
                        o.Decorate(c => c.Get<IBus>());
                    })
                    .Start();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

                Assert.IsType<InvalidOperationException>(exception.InnerException);
                Assert.Equal("oh no!! THIS is the actual exception - everything else is noise", exception.InnerException.Message);
            }
        }
    }
}