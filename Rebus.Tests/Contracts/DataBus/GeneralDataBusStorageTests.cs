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

        protected override void SetUp()
        {
            _storage = new TDataStorageFactory().Create();
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
                await _storage.Read(knownId).CopyToAsync(destination);

                var readData = Encoding.UTF8.GetString(destination.ToArray());

                Assert.That(readData, Is.EqualTo(originalData));
            }
        }

        [Test]
        public async Task CanLoadSavedDataMultipleTimes()
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
                await _storage.Read(knownId).CopyToAsync(destination);

                var readData = Encoding.UTF8.GetString(destination.ToArray());

                Assert.That(readData, Is.EqualTo(originalData));
            }
        }
    }
}