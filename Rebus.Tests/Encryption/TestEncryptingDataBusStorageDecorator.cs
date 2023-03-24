using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Encryption;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Encryption;

[TestFixture]
public class TestEncryptingDataBusStorageDecorator : FixtureBase
{
    [Test]
    public async Task Hello()
    {
        const string text = "HEJ MED DIG MIN VEN! 🙂";

        var dataStore = new InMemDataStore();
        var receivedPayloads = new ConcurrentQueue<string>();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<DataBusAttachment>(async (bus, context, message) =>
        {
            await using var source = await message.OpenRead();
            await using var target = new MemoryStream();
            await source.CopyToAsync(target);
            receivedPayloads.Enqueue(Encoding.UTF8.GetString(target.ToArray()));
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "encrypted-data-bus"))
            .Options(o => o.EnableEncryption("aPgrRcTBF14hrdPmKgNmoTI+N84fdtxwd+DHb2wNtRo="))
            .DataBus(d =>
            {
                d.StoreInMemory(dataStore);

                d.EnableEncryption();
            })
            .Start();

        using var source = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var attachment = await activator.Bus.Advanced.DataBus.CreateAttachment(source);
        await activator.Bus.SendLocal(attachment);

        await receivedPayloads.WaitUntil(q => q.Count == 1);
        var receivedPayload = receivedPayloads.First();

        Assert.That(receivedPayload, Is.EqualTo(text));

        var data = dataStore.Load(attachment.Id) ?? throw new ArgumentException($"Could not find attachment with ID '{attachment.Id}' in the data store");
        Assert.That(Encoding.UTF8.GetString(data), Is.Not.EqualTo(text));
    }
}