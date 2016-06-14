using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.DataBus;
using Rebus.Tests.Extensions;

namespace Rebus.Tests.Contracts.DataBus
{
    public abstract class GeneralDataBusStorageTests<TDataStorageFactory> : FixtureBase where TDataStorageFactory: IDataBusStorageFactory, new()
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