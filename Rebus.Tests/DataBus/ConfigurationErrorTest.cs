using System;
using Rebus.Activation;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.DataBus
{
    public class ConfigurationErrorTest : FixtureBase
    {
        [Fact]
        public void ThrowsAppropriateExceptionWhenMissingStorageConfiguration()
        {
            try
            {
                Configure.With(Using(new BuiltinHandlerActivator()))
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bim"))
                    .Options(o => o.EnableDataBus())
                    .Start();
            }
            catch (Exception exception)
            {
                var errorMessage = exception.ToString();

                Console.WriteLine(errorMessage);

                Assert.Contains("did you call 'EnableDataBus' without choosing a way to store the data", errorMessage);
            }
        }
    }
}