using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Persistence.FileSystem;
using Rebus.Serialization;
using Rebus.Time;
// ReSharper disable UnusedVariable
// ReSharper disable PossibleNullReferenceException
// ReSharper disable EmptyGeneralCatchClause
// ReSharper disable ArgumentsStyleAnonymousFunction
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.DataBus.FileSystem;

/// <summary>
/// Implementation of <see cref="IDataBusStorage"/> that stores data in the file system. Could be a directory on a network share.
/// </summary>
public class FileSystemDataBusStorage : IDataBusStorage, IDataBusStorageManagement, IInitializable
{
    const string DataFileExtension = "dat";
    const string MetadataFileExtension = "meta";
    const string FilePrefix = "data-";

    readonly DictionarySerializer _dictionarySerializer = new DictionarySerializer();
    readonly string _directoryPath;
    readonly IRebusTime _rebusTime;
    readonly Retrier _retrier;
    readonly ILog _log;

    /// <summary>
    /// Creates the data storage
    /// </summary>
    public FileSystemDataBusStorage(string directoryPath, IRebusLoggerFactory rebusLoggerFactory, IRebusTime rebusTime)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _directoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
        _log = rebusLoggerFactory.GetLogger<FileSystemDataBusStorage>();
        _retrier = new Retrier(rebusLoggerFactory);
    }

    /// <summary>
    /// Initializes the file system data storage by ensuring that the configured data directory path exists and that it is writable
    /// </summary>
    public void Initialize()
    {
        if (!Directory.Exists(_directoryPath))
        {
            _log.Info("Creating directory {directoryPath}", _directoryPath);
            Directory.CreateDirectory(_directoryPath);
        }

        _log.Info("Checking that the current process has read/write access to directory {directoryPath}", _directoryPath);
        FileSystemHelpers.EnsureDirectoryIsWritable(_directoryPath);
    }

    /// <summary>
    /// Saves the data from the given strea under the given ID
    /// </summary>
    public async Task Save(string id, Stream source, Dictionary<string, string> metadata = null)
    {
        var filePath = GetFilePath(id, DataFileExtension);

        using (var destination = File.Create(filePath))
        {
            await source.CopyToAsync(destination);
        }

        var metadataToSave = new Dictionary<string, string>(metadata ?? new Dictionary<string, string>())
        {
            [MetadataKeys.SaveTime] = _rebusTime.Now.ToString("O")
        };
        var metadataFilePath = GetFilePath(id, MetadataFileExtension);

        using (var destination = File.Create(metadataFilePath))
        using (var writer = new StreamWriter(destination, Encoding.UTF8))
        {
            var text = _dictionarySerializer.SerializeToString(metadataToSave);
            await writer.WriteAsync(text);
        }
    }

    /// <summary>
    /// Reads the data with the given ID and returns it as a stream
    /// </summary>
    public async Task<Stream> Read(string id)
    {
        var filePath = GetFilePath(id, DataFileExtension);

        try
        {
            var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            await UpdateLastReadTime(id);

            return fileStream;
        }
        catch (FileNotFoundException exception)
        {
            throw new ArgumentException($"Could not find file for data with ID {id}", exception);
        }
    }

    /// <summary>
    /// Loads the metadata stored with the given ID
    /// </summary>
    public async Task<Dictionary<string, string>> ReadMetadata(string id)
    {
        var filePath = GetFilePath(id, DataFileExtension);
        var metadataFilePath = GetFilePath(id, MetadataFileExtension);

        return InnerReadMetadata(id, metadataFilePath, filePath);
    }

    /// <summary>
    /// Deletes the attachment with the given ID
    /// </summary>
    public async Task Delete(string id)
    {
        var filePath = GetFilePath(id, DataFileExtension);
        var metadataFilePath = GetFilePath(id, MetadataFileExtension);

        _retrier.Execute(
            action: () => InnerDelete(id, filePath, metadataFilePath),
            handle: ex => ex is IOException,
            attempts: 10
        );
    }

    static void InnerDelete(string id, string filePath, string metadataFilePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (FileNotFoundException)
        {
        }
        catch (Exception exception)
        {
            throw new IOException($"Could not delete file for data with ID {id}", exception);
        }

        try
        {
            File.Delete(metadataFilePath);
        }
        catch (FileNotFoundException)
        {
        }
        catch (Exception exception)
        {
            throw new IOException($"Could not delete file for metadata with ID {id}", exception);
        }
    }

    /// <summary>
    /// Iterates through IDs of attachments that match the given <paramref name="readTime"/> and <paramref name="saveTime"/> criteria.
    /// </summary>
    public IEnumerable<string> Query(TimeRange readTime = null, TimeRange saveTime = null)
    {
        var metadataFilePaths = Directory.EnumerateFiles(_directoryPath, $"*.{MetadataFileExtension}");

        foreach (var metadataFilePath in metadataFilePaths)
        {
            string id;

            try
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(metadataFilePath);

                id = fileNameWithoutExtension.Substring(FilePrefix.Length);

                var metadata = InnerReadMetadata(id, metadataFilePath, GetFilePath(id, DataFileExtension));

                if (!DataBusStorageQuery.IsSatisfied(metadata, readTime, saveTime))
                {
                    continue;
                }
            }
            catch (Exception)
            {
                id = null;
            }

            if (id != null)
            {
                yield return id;
            }
        }
    }

    async Task UpdateLastReadTime(string id)
    {
        var metadataFilePath = GetFilePath(id, MetadataFileExtension);

        try
        {
            using (var file = File.Open(metadataFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var reader = new StreamReader(file, Encoding.UTF8))
                {
                    var jsonText = await reader.ReadToEndAsync();
                    var metadata = _dictionarySerializer.DeserializeFromString(jsonText);

                    metadata[MetadataKeys.ReadTime] = _rebusTime.Now.ToString("O");

                    var newJsonText = _dictionarySerializer.SerializeToString(metadata);

                    file.Position = 0;

                    using (var writer = new StreamWriter(file, Encoding.UTF8))
                    {
                        await writer.WriteAsync(newJsonText);
                    }
                }
            }
        }
        catch (IOException)
        {
            // this exception is most likely caused by a locked file because someone else is updating the
            // last read time  - that's ok :)
        }
        catch (Exception exception)
        {
            throw new RebusApplicationException(exception, $"Could not update metadata for data with ID {id}");
        }
    }

    string GetFilePath(string id, string extension) => Path.Combine(_directoryPath, $"{FilePrefix}{id}.{extension}");

    Dictionary<string, string> InnerReadMetadata(string id, string metadataFilePath, string filePath)
    {
        try
        {
            using (var fileStream = File.OpenRead(metadataFilePath))
            {
                using (var reader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    var jsonText = reader.ReadToEnd();
                    var metadata = _dictionarySerializer.DeserializeFromString(jsonText);
                    var fileInfo = new FileInfo(filePath);

                    metadata[MetadataKeys.Length] = fileInfo.Length.ToString();

                    return metadata;
                }
            }
        }
        catch (Exception exception)
        {
            throw new RebusApplicationException(exception, $"Could not read metadata for data with ID {id}");
        }
    }
}