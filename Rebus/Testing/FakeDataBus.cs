using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rebus.DataBus;
using Rebus.DataBus.InMem;

namespace Rebus.Testing
{
    /// <summary>
    /// Test helper that can be used to fake the presence of a configured data bus, using the given in-mem data store to store data
    /// </summary>
    public class FakeDataBus : IDataBus
    {
        internal static IDataBusStorage TestDataBusStorage;

        readonly IDataBusStorage _dataBusStorage;

        /// <summary>
        /// Establishes a fake presence of a configured data bus, using the given <see cref="InMemDataStore"/> to retrieve data
        /// </summary>
        public static IDisposable EstablishContext(InMemDataStore dataStore)
        {
            if (dataStore == null) throw new ArgumentNullException(nameof(dataStore));

            TestDataBusStorage = new InMemDataBusStorage(dataStore);

            return new CleanUp(() =>
            {
                TestDataBusStorage = null;
            });
        }

        /// <summary>
        /// Creates the fake data bus, optionally using the given in-mem data store to store attachments
        /// </summary>
        /// <param name="dataStore"></param>
        public FakeDataBus(InMemDataStore dataStore = null)
        {
            // if a data store was passed in, we use that
            if (dataStore != null)
            {
                _dataBusStorage = new InMemDataBusStorage(dataStore);
            }
            // otherwise, if there is an "ambient" storage, use that
            else if (TestDataBusStorage != null)
            {
                _dataBusStorage = TestDataBusStorage;
            }
            // last resort: just fake it in mem
            else
            {
                _dataBusStorage = new InMemDataBusStorage(new InMemDataStore());
            }
        }

        /// <inheritdoc />
        public async Task<DataBusAttachment> CreateAttachment(Stream source, Dictionary<string, string> optionalMetadata = null)
        {
            var id = Guid.NewGuid().ToString();

            await _dataBusStorage.Save(id, source, optionalMetadata);

            return new DataBusAttachment(id);
        }

        class CleanUp : IDisposable
        {
            readonly Action _disposeAction;

            public CleanUp(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }
            public void Dispose()
            {
                _disposeAction();
            }
        }
    }
}