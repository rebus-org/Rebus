using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.DataBus;

[TestFixture]
public class OutOfHandlersRoundtripTest : FixtureBase
{
    IBus _bus;

    protected override void SetUp()
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        _bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "data-tripping"))
            .DataBus(d => d.StoreInMemory(new InMemDataStore()))
            .Start();
    }

    [Test]
    public async Task RoundtripSomeData()
    {
        var knownData = "HELLO THERE MY FRIEND";
        var dataBus = _bus.Advanced.DataBus;
        var attachment = await CreateAttachment(dataBus, knownData);
        var id = attachment.Id;

        var fullyRoundtrippedKnownData = await LoadAttachment(dataBus, id);

        Assert.That(fullyRoundtrippedKnownData, Is.EqualTo(knownData));
    }

    [Test]
    public async Task RoundtripSomeMetaData()
    {
        var knownKey = "KEY";
        var knownValue = "HELLO THERE MY FRIEND";
        var dataBus = _bus.Advanced.DataBus;
        var attachment = await CreateAttachment(dataBus, "blah", new Dictionary<string, string>
        {
            {knownKey, knownValue}
        });
        var id = attachment.Id;

        var fullyRoundtrippedMetadata = await dataBus.GetMetadata(id);

        Assert.That(fullyRoundtrippedMetadata, Contains.Key(knownKey));
        Assert.That(fullyRoundtrippedMetadata[knownKey], Is.EqualTo(knownValue));
    }

    static async Task<string> LoadAttachment(IDataBus dataBus, string id)
    {
        using (var destination = new MemoryStream())
        {
            using (var source = await dataBus.OpenRead(id))
            {
                await source.CopyToAsync(destination);
            }

            return Encoding.UTF8.GetString(destination.ToArray());
        }
    }

    static async Task<DataBusAttachment> CreateAttachment(IDataBus dataBus, string text, Dictionary<string, string> optionalMetadata = null)
    {
        using (var source = new MemoryStream(Encoding.UTF8.GetBytes(text)))
        {
            return await dataBus.CreateAttachment(source, optionalMetadata);
        }
    }
}