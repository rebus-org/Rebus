using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Compression;
using Rebus.DataBus.InMem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Time;

namespace Rebus.Tests.DataBus.Zip;

[TestFixture]
public class TestZippingDataBusStorageDecorator : FixtureBase
{
    ZippingDataBusStorageDecorator _zipStorage;
    Random _random;

    protected override void SetUp()
    {
        _random = new Random(DateTime.Now.GetHashCode());

        var storage = new InMemDataBusStorage(new InMemDataStore(), new FakeRebusTime());

        _zipStorage = new ZippingDataBusStorageDecorator(storage, DataCompressionMode.Always);
    }

    [TestCase(1024)]
    [TestCase(10 * 1024)]
    [Repeat(3)]
    public async Task RoundtripTest(int maxBufferSizeKb)
    {
        const string id = "known-id";
        var buffer = GetRandomAmountOfRandomData(maxBufferSizeKb);

        Console.WriteLine($"Checking {buffer.Length} bytes of good stuff");

        using (var source = new MemoryStream(buffer))
        {
            await _zipStorage.Save(id, source);
        }

        using (var source = await _zipStorage.Read(id))
        {
            using (var destination = new MemoryStream())
            {
                await source.CopyToAsync(destination);

                var roundtrippedBuffer = destination.ToArray();

                Assert.That(roundtrippedBuffer.Length, Is.EqualTo(buffer.Length));

                for (var index = 0; index < roundtrippedBuffer.Length; index++)
                {
                    Assert.That(roundtrippedBuffer[index], Is.EqualTo(buffer[index]));
                }
            }
        }
    }

    byte[] GetRandomAmountOfRandomData(int maxBufferSizeKb)
    {
        var buffer = new byte[_random.Next(maxBufferSizeKb) * 1024];
        _random.NextBytes(buffer);
        return buffer;
    }
}