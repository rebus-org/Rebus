using System;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.DataBus;

[TestFixture]
public class ConfigurationErrorTest : FixtureBase
{
    [Test]
    public void ThrowsAppropriateExceptionWhenMissingStorageConfiguration()
    {
        try
        {
            Configure.With(Using(new BuiltinHandlerActivator()))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bim"))
                .DataBus(o => { })
                .Start();
        }
        catch (Exception exception)
        {
            var errorMessage = exception.ToString();

            Console.WriteLine(errorMessage);

            Assert.That(errorMessage, Contains.Substring("did you call 'EnableDataBus' without choosing a way to store the data"));
        }
    }
}