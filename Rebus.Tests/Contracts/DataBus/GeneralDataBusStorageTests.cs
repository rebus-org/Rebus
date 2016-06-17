using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Rebus.DataBus;
using Rebus.Tests.Extensions;
using Rebus.Time;

namespace Rebus.Tests.Contracts.DataBus
{
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
        public void ThrowsWhenLoadingNonExistentId()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                _storage.Read(Guid.NewGuid().ToString());
            });

            Console.WriteLine(exception);
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

            Assert.That(readMetadata[MetadataKeys.Length], Is.EqualTo("3"));
            Assert.That(readMetadata[MetadataKeys.SaveTime], Is.EqualTo(fakeTime.ToString("O")));
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
                using (var source = _storage.Read(knownId))
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
                using (var source = _storage.Read(knownId))
                {
                    await source.CopyToAsync(destination);
                }

                var readData = Encoding.UTF8.GetString(destination.ToArray());

                Assert.That(readData, Is.EqualTo(originalData));
            }
        }
    }
}