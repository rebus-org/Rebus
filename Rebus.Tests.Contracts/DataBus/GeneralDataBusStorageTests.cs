using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Rebus.Compression;
using Rebus.DataBus;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Time;
using Xunit;

namespace Rebus.Tests.Contracts.DataBus
{
    /// <summary>
    /// Test fixture base class for verifying compliance with the <see cref="IDataBusStorage"/> contract
    /// </summary>
    public abstract class GeneralDataBusStorageTests<TDataStorageFactory> : FixtureBase where TDataStorageFactory : IDataBusStorageFactory, new()
    {
        IDataBusStorage _storage;
        TDataStorageFactory _factory;

        protected GeneralDataBusStorageTests()
        {
            _factory = new TDataStorageFactory();
            _storage = _factory.Create();
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Fact]
        public async Task UpdatesTimeOfLastRead()
        {
            const string knownId = "known id";

            using (var source = new MemoryStream(new byte[0]))
            {
                await _storage.Save(knownId, source);
            }

            var hadLastReadTime = (await _storage.ReadMetadata(knownId)).ContainsKey(MetadataKeys.ReadTime);

            Assert.False(hadLastReadTime, $"Did not expect the {MetadataKeys.ReadTime} key to be set");

            var justSomeTime = new DateTimeOffset(1.January(2016));


            RebusTimeMachine.FakeIt(justSomeTime);

            _storage.Read(knownId).Result.Dispose();

            var metadata = await _storage.ReadMetadata(knownId);

            Assert.True(metadata.ContainsKey(MetadataKeys.ReadTime));
            Assert.Equal(justSomeTime.ToString("O"), metadata[MetadataKeys.ReadTime]);
        }

        [Fact]
        public void ThrowsWhenLoadingNonExistentId()
        {
            var exception = Assert.Throws<AggregateException>(() =>
            {
                var result = _storage.Read(Guid.NewGuid().ToString()).Result;
            });

            var baseException = exception.GetBaseException();

            Console.WriteLine(baseException);

            Assert.IsType<ArgumentException>(baseException);
        }

       [Fact]
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

            Assert.Equal("value1", readMetadata["key1"]);
            Assert.Equal("value2", readMetadata["key2"]);
        }

       [Fact]
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
                Assert.Equal("23", readMetadata[MetadataKeys.Length]);
            }
            else
            {
                Assert.Equal("3", readMetadata[MetadataKeys.Length]);
            }
            Assert.Equal(fakeTime.ToString("O"), readMetadata[MetadataKeys.SaveTime]);
        }

       [Fact]
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

                Assert.Equal(originalData, readData);
            }
        }

       [Fact]
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

                Assert.Equal(originalData, readData);
            }
        }

       [Fact]
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

                Assert.Equal(originalData, readData);
            }
        }
    }
}