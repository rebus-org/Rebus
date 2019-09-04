using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Compression;
using Rebus.DataBus;
using Rebus.Extensions;
using Rebus.Tests.Contracts.Extensions;
// ReSharper disable ArgumentsStyleOther

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
        public async Task CanQueryByTimeStamps_ReadTime()
        {
            async Task CreateAttachment(DateTime readTime, string attachmendId)
            {
                _factory.FakeIt(DateTimeOffset.Now);
                using (var source = new MemoryStream(new byte[0]))
                {
                    await _storage.Save(attachmendId, source);
                }

                // update read time
                _factory.FakeIt(readTime);
                using (await _storage.Read(attachmendId))
                {
                }
            }

            await CreateAttachment(new DateTime(2019, 01, 01), "id1");
            await CreateAttachment(new DateTime(2019, 02, 01), "id2");
            await CreateAttachment(new DateTime(2019, 03, 01), "id3");
            await CreateAttachment(new DateTime(2019, 04, 01), "id4");
            await CreateAttachment(new DateTime(2019, 05, 01), "id5");

            var ids1 = _storage.Query(readTime: new TimeRange(from: new DateTime(2019, 02, 01))).InOrder().ToList();
            var ids2 = _storage.Query(readTime: new TimeRange(from: new DateTime(2019, 03, 01))).InOrder().ToList();
            var ids3 = _storage.Query(readTime: new TimeRange(from: new DateTime(2019, 03, 01), to: new DateTime(2019, 05, 01))).InOrder().ToList();

            Assert.That(ids1, Is.EqualTo(new[] { "id2", "id3", "id4", "id5" }));
            Assert.That(ids2, Is.EqualTo(new[] { "id3", "id4", "id5" }));
            Assert.That(ids3, Is.EqualTo(new[] { "id3", "id4" }));
        }

        [Test]
        public async Task CanQueryByTimeStamps_SaveTime()
        {
            async Task CreateAttachment(DateTime saveTime, string attachmendId)
            {
                _factory.FakeIt(new DateTimeOffset(saveTime));

                using (var source = new MemoryStream(new byte[0]))
                {
                    await _storage.Save(attachmendId, source);
                }
            }

            await CreateAttachment(new DateTime(2019, 01, 01), "id1");
            await CreateAttachment(new DateTime(2019, 02, 01), "id2");
            await CreateAttachment(new DateTime(2019, 03, 01), "id3");
            await CreateAttachment(new DateTime(2019, 04, 01), "id4");
            await CreateAttachment(new DateTime(2019, 05, 01), "id5");

            var ids1 = _storage.Query(saveTime: new TimeRange(from: new DateTime(2019, 02, 01))).InOrder().ToList();
            var ids2 = _storage.Query(saveTime: new TimeRange(from: new DateTime(2019, 03, 01))).InOrder().ToList();
            var ids3 = _storage.Query(saveTime: new TimeRange(from: new DateTime(2019, 03, 01), to: new DateTime(2019, 05, 01))).InOrder().ToList();

            Assert.That(ids1, Is.EqualTo(new[] { "id2", "id3", "id4", "id5" }));
            Assert.That(ids2, Is.EqualTo(new[] { "id3", "id4", "id5" }));
            Assert.That(ids3, Is.EqualTo(new[] { "id3", "id4" }));
        }

        [Test]
        public async Task CanDeleteAttachment()
        {
            const string knownId = "known id";

            using (var source = new MemoryStream(new byte[0]))
            {
                await _storage.Save(knownId, source);
            }

            using (var sourceStream = await _storage.Read(knownId))
            {
                Assert.That(sourceStream, Is.Not.Null);
            }

            await _storage.Delete(knownId);

            var exception = Assert.ThrowsAsync<ArgumentException>(() => _storage.Read(knownId));

            Console.WriteLine(exception);

            Assert.That(exception.ToString(), Contains.Substring(knownId));
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

            var justSomeTime = new DateTimeOffset(new DateTime(2016, 1, 1));

            _factory.FakeIt(justSomeTime);

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
            var fakeTime = new DateTimeOffset(new DateTime(2016, 6, 17));

            _factory.FakeIt(fakeTime);

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