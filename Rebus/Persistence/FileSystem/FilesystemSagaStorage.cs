using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Sagas;

namespace Rebus.Persistence.FileSystem
{
    public class FilesystemSagaStorage : ISagaStorage
    {
        private readonly string _basePath;
        private readonly IRebusLoggerFactory _rebusLoggerFactory;
        private readonly string _lockFile;
        const string IdPropertyName = nameof(ISagaData.Id);
        

        public FilesystemSagaStorage(string basePath, IRebusLoggerFactory rebusLoggerFactory)
        {
            _basePath = basePath;
            _rebusLoggerFactory = rebusLoggerFactory;
            _lockFile = Path.Combine(basePath, "lock.txt");
            FilesystemExclusiveLock.EnsureTargetFile(basePath, rebusLoggerFactory);
        }



        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            using (new FilesystemExclusiveLock(_lockFile))
            {
                var index = new FilesystemSagaIndex(_basePath);
                if (propertyName == IdPropertyName)
                {
                    return index.Find((Guid) propertyValue);
                }
                return index.Find(sagaDataType, propertyName, propertyValue);
            }
        }
        static Guid GetId(ISagaData sagaData)
        {
            var id = sagaData.Id;

            if (id != Guid.Empty) return id;

            throw new InvalidOperationException("Saga data must be provided with an ID in order to do this!");
        }
        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            using (new FilesystemExclusiveLock(_lockFile))
            {
                var index = new FilesystemSagaIndex(_basePath);
                var id = GetId(sagaData);
                if (sagaData.Revision != 0)
                {
                    throw new InvalidOperationException($"Attempted to insert saga data with ID {id} and revision {sagaData.Revision}, but revision must be 0 on first insert!");

                }
                var existingSaga = index.Find(id);
                if (existingSaga != null)
                {
                    throw new ConcurrencyException("Saga data with ID {0} already exists!", id);
                }
                index.Insert(sagaData, correlationProperties);

            }
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            using (new FilesystemExclusiveLock(_lockFile))
            {
                var index = new FilesystemSagaIndex(_basePath);
                var id = GetId(sagaData);
                var existingCopy = index.Find(id);
                if (existingCopy == null)
                {
                    throw new ConcurrencyException("Saga data with ID {0} does not exist!", id);
                }
                if (existingCopy.Revision != sagaData.Revision)
                {
                    throw new ConcurrencyException("Attempted to update saga data with ID {0} with revision {1}, but the existing data was updated to revision {2}",
                        id, sagaData.Revision, existingCopy.Revision);
                }
                sagaData.Revision++;
                index.Insert(sagaData, correlationProperties);
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            using (new FilesystemExclusiveLock(_lockFile))
            {
                var index = new FilesystemSagaIndex(_basePath);
                var id = sagaData.Id;
                if (!index.Contains(id))
                {
                    throw new ConcurrencyException("Saga data with ID {0} no longer exists and cannot be deleted", id);
                }
                index.Remove(id);
            }
        }



    }
}
