using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Rebus.Compression;
using Rebus.DataBus;
using Rebus.Extensions;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Time;

namespace Rebus.Tests.Contracts.DataBus
{
    /// <summary>
    /// Test fixture base class for verifying compliance with the <see cref="IDataBusStorage"/> contract
    /// </summary>
    public abstract class GeneralDataBusStorageTests<TDataStorageFactory> : FixtureBase where TDataStorageFactory : IDataBusStorageFactory, new()
    {
        IDataBusStorage _storage;
        TDataStorageFactory _factory;

        protected override void SetUp()
        {
            _factory = new TDataStorageFactory();
            _storage = _factory.Create();
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Test]
        public async Task UpdatesTimeOfLastRead()
        {
            const string knownId = "known id";

            using (var source = new MemoryStream(new byte[0]))
            {
                await _storage.Save(knownId, source);
            }

            var hadLastReadTime = (await _storage.ReadMetadata(knownId)).ContainsKey(MetadataKeys.ReadTime);

            Assert.That(hadLastReadTime, Is.False, "Did not expect the {0} key to be set", MetadataKeys.ReadTime);

            var justSomeTime = new DateTimeOffset(1.January(2016));

            RebusTimeMachine.FakeIt(justSomeTime);

            _storage.Read(knownId).Result.Dispose();

            var metadata = await _storage.ReadMetadata(knownId);

            Assert.That(metadata.ContainsKey(MetadataKeys.ReadTime), Is.True);

            var readTimeMetadata = metadata[MetadataKeys.ReadTime];
            var readTime = DateTimeOffset.Parse(readTimeMetadata);

            Assert.That(readTime, Is.EqualTo(justSomeTime),
                $"Expected that the '{MetadataKeys.ReadTime}' metadata value '{readTimeMetadata}' would equal {justSomeTime} when passed to DateTimeOffset.Parse(...)");
        }

        [Test]
        public void ThrowsWhenLoadingNonExistentId()
        {
            var exception = Assert.Throws<AggregateException>(() =>
            {
                var result = _storage.Read(Guid.NewGuid().ToString()).Result;
            });

            var baseException = exception.GetBaseException();

            Console.WriteLine(baseException);

            Assert.That(baseException, Is.TypeOf<ArgumentException>());
        }

        [Test]
        public async Task CanSaveDataAlongWithCustomMetadata()
        {
            const string knownId = "known id";

            var medadada = new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"},
            };

            using (var source = new MemoryStream(new byte[0]))
            {
                await _storage.Save(knownId, source, medadada);
            }

            var readMetadata = await _storage.ReadMetadata(knownId);

            Assert.That(readMetadata["key1"], Is.EqualTo("value1"));
            Assert.That(readMetadata["key2"], Is.EqualTo("value2"));
        }

        [Test]
        public async Task CanGetStandardMetada()
        {
            var fakeTime = new DateTimeOffset(17.June(2016));
            RebusTimeMachine.FakeIt(fakeTime);

            const string knownId = "known id";

            var data = new byte[] { 1, 2, 3 };

            using (var source = new MemoryStream(data))
            {
                await _storage.Save(knownId, source);
            }

            var readMetadata = await _storage.ReadMetadata(knownId);

            // special case: zipped data has different size (and is actually bigger in this case :))
            if (_storage is ZippingDataBusStorageDecorator)
            {
                Assert.That(readMetadata.GetValue(MetadataKeys.Length), Is.EqualTo("23"));
            }
            else
            {
                Assert.That(readMetadata.GetValue(MetadataKeys.Length), Is.EqualTo("3"));
            }
            Assert.That(readMetadata.GetValue(MetadataKeys.SaveTime), Is.EqualTo(fakeTime.ToString("O")));
        }

        [Test]
        public async Task CanSaveAndLoadData()
        {
            const string knownId = "known id";
            const string originalData = "this is some data";

            using (var source = new MemoryStream(Encoding.UTF8.GetBytes(originalData)))
            {
                await _storage.Save(knownId, source);
            }

            using (var destination = new MemoryStream())
            {
                using (var source = await _storage.Read(knownId))
                {
                    await source.CopyToAsync(destination);
                }

                var readData = Encoding.UTF8.GetString(destination.ToArray());

                Assert.That(readData, Is.EqualTo(originalData));
            }
        }

        [Test]
        public async Task CanSaveAndLoadBiggerPieceOfData()
        {
            const string knownId = "known id";

            var originalData = string.Join("/", Enumerable.Range(0, 10000));

            using (var source = new MemoryStream(Encoding.UTF8.GetBytes(originalData)))
            {
                await _storage.Save(knownId, source);
            }

            using (var destination = new MemoryStream())
            {
                using (var source = await _storage.Read(knownId))
                {
                    await source.CopyToAsync(destination);
                }

                var readData = Encoding.UTF8.GetString(destination.ToArray());

                Assert.That(readData, Is.EqualTo(originalData));
            }
        }

        [Test]
        public async Task CanLoadSaveDataMultipleTimes()
        {
            const string knownId = "known id";
            const string originalData = "this is some data";

            using (var source = new MemoryStream(Encoding.UTF8.GetBytes(originalData)))
            {
                await _storage.Save(knownId, source);
            }

            100.Times(() =>
            {
                AssertCanRead(knownId, originalData).Wait();
            });
        }

        async Task AssertCanRead(string knownId, string originalData)
        {
            using (var destination = new MemoryStream())
            {
                using (var source = await _storage.Read(knownId))
                {
                    await source.CopyToAsync(destination);
                }

                var readData = Encoding.UTF8.GetString(destination.ToArray());

                Assert.That(readData, Is.EqualTo(originalData));
            }
        }
    }
}