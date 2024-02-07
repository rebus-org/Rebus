using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Sagas;
#pragma warning disable 1998

namespace Rebus.Persistence.FileSystem;

/// <summary>
/// Implementation of <see cref="ISagaStorage"/> that uses the file system to store data
/// </summary>
public class FileSystemSagaStorage : ISagaStorage
{
    const string IdPropertyName = nameof(ISagaData.Id);
    readonly string _basePath;
    readonly string _lockFile;
    readonly ILog _log;

    /// <summary>
    /// Creates the saga storage using the given <paramref name="basePath"/> 
    /// </summary>
    public FileSystemSagaStorage(string basePath, IRebusLoggerFactory rebusLoggerFactory)
    {
        if (basePath == null) throw new ArgumentNullException(nameof(basePath));
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _basePath = basePath;
        _log = rebusLoggerFactory.GetLogger<FileSystemSagaStorage>();
        _lockFile = Path.Combine(basePath, "lock.txt");
    }

    /// <summary>
    /// Looks up an existing saga data instance from the index file
    /// </summary>
    public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
    {
        using (new FileSystemExclusiveLock(_lockFile, _log))
        {
            var index = new FileSystemSagaIndex(_basePath);
            if (propertyName == IdPropertyName)
            {
                if (propertyValue == null) return null;

                var guidPropertyValue = GetGuidValue(propertyValue);
                var sagaData = index.FindById(guidPropertyValue);

                if (!sagaDataType.IsInstanceOfType(sagaData))
                {
                    return null;
                }

                return sagaData;
            }
            return index.Find(sagaDataType, propertyName, propertyValue);
        }

        Guid GetGuidValue(object value)
        {
            if (value is Guid guid) return guid;

            if (value is string {Length: > 0} str)
            {
                try
                {
                    return Guid.Parse(str);
                }
                catch (Exception exception)
                {
                    throw new FormatException($"Could not parse the string value '{str}' into a GUID", exception);
                }
            }

            throw new InvalidCastException($"The value {value} is not a Guid or a string-encoded Guid");
        }
    }

    /// <summary>
    /// Inserts the given saga data instance into the index file
    /// </summary>
    public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
    {
        using (new FileSystemExclusiveLock(_lockFile, _log))
        {
            var index = new FileSystemSagaIndex(_basePath);
            var id = GetId(sagaData);
            if (sagaData.Revision != 0)
            {
                throw new InvalidOperationException($"Attempted to insert saga data with ID {id} and revision {sagaData.Revision}, but revision must be 0 on first insert!");

            }
            var existingSaga = index.FindById(id);
            if (existingSaga != null)
            {
                throw new ConcurrencyException($"Saga data with ID {id} already exists!");
            }
            index.Insert(sagaData, correlationProperties);

        }
    }

    /// <summary>
    /// Updates the given saga data instance in the index file
    /// </summary>
    public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
    {
        using (new FileSystemExclusiveLock(_lockFile, _log))
        {
            var index = new FileSystemSagaIndex(_basePath);
            var id = GetId(sagaData);
            var existingCopy = index.FindById(id);
            if (existingCopy == null)
            {
                throw new ConcurrencyException($"Saga data with ID {id} does not exist!");
            }
            if (existingCopy.Revision != sagaData.Revision)
            {
                throw new ConcurrencyException($"Attempted to update saga data with ID {id} with revision {sagaData.Revision}, but the existing data was updated to revision {existingCopy.Revision}");
            }
            sagaData.Revision++;
            index.Insert(sagaData, correlationProperties);
        }
    }

    /// <summary>
    /// Removes the saga data instance from the index file
    /// </summary>
    public async Task Delete(ISagaData sagaData)
    {
        using (new FileSystemExclusiveLock(_lockFile, _log))
        {
            var index = new FileSystemSagaIndex(_basePath);
            var id = sagaData.Id;
            if (!index.Contains(id))
            {
                throw new ConcurrencyException($"Saga data with ID {id} no longer exists and cannot be deleted");
            }
            index.Remove(id);
            sagaData.Revision++;
        }
    }

    static Guid GetId(ISagaData sagaData)
    {
        var id = sagaData.Id;

        if (id != Guid.Empty) return id;

        throw new InvalidOperationException("Saga data must be provided with an ID in order to do this!");
    }
}