using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests;

namespace Rebus.DataBus.Tests
{
    [TestFixture]
    public class TestInMemDataBusStorage : FixtureBase
    {
        [Test]
        public async Task CanSaveAndLoadData()
        {
            var dataStorage = new FakeDataBusTestExtensions.InMemDataBusStorage();

            const string knownId = "known id";
            const string originalData = "this is some data";

            using (var source = new MemoryStream(Encoding.UTF8.GetBytes(originalData)))
            {
                await dataStorage.Save(knownId, source);
            }

            using (var destination = new MemoryStream())
            {
                await dataStorage.Load(knownId, destination);

                var readData = Encoding.UTF8.GetString(destination.ToArray());

                Assert.That(readData, Is.EqualTo(originalData));
            }
        }

    }
}