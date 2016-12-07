using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Serialization;
using Rebus.Time;

namespace Rebus.DataBus.FileSystem
{
    /// <summary>
    /// Implementation of <see cref="IDataBusStorage"/> that stores data in the file system. Could be a directory on a network share.
    /// </summary>
    public class FileSystemDataBusStorage : IDataBusStorage, IInitializable
    {
        const string DataFileExtension = "dat";
        const string MetadataFileExtension = "meta";
        readonly DictionarySerializer _dictionarySerializer = new DictionarySerializer();
        readonly string _directoryPath;
        readonly ILog _log;

        /// <summary>
        /// Creates the data storage
        /// </summary>
        public FileSystemDataBusStorage(string directoryPath, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (directoryPath == null) throw new ArgumentNullException(nameof(directoryPath));
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _directoryPath = directoryPath;
            _log = rebusLoggerFactory.GetCurrentClassLogger();
        }

        /// <summary>
        /// Initializes the file system data storage by ensuring that the configured data directory path exists and that it is writable
        /// </summary>
        public void Initialize()
        {
            if (!Directory.Exists(_directoryPath))
            {
                _log.Info("Creating directory {0}", _directoryPath);
                Directory.CreateDirectory(_directoryPath);
            }

            _log.Info("Checking that the current process has read/write access to directory {0}", _directoryPath);
            EnsureDirectoryIsWritable();
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
                [MetadataKeys.SaveTime] = RebusTime.Now.ToString("O")
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

            try
            {
                using (var fileStream = File.OpenRead(metadataFilePath))
                {
                    using (var reader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        var jsonText = await reader.ReadToEndAsync();
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

                        metadata[MetadataKeys.ReadTime] = RebusTime.Now.ToString("O");

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

        string GetFilePath(string id, string extension)
        {
            return Path.Combine(_directoryPath, $"data-{id}.{extension}");
        }

        void EnsureDirectoryIsWritable()
        {
            var now = DateTime.Now;
            var filePath = Path.Combine(_directoryPath, $"write-test-{now:yyyyMMdd}-{now:HHmmss}-DELETE-ME.tmp");

            try
            {
                const string contents =
                    @"Wrote this file to be sure that this Rebus endpoint has read/write access to this directory.

This file can be safely deleted.";

                File.WriteAllText(filePath, contents, Encoding.UTF8);

                var readAllText = File.ReadAllText(filePath, Encoding.UTF8);

            }
            catch (Exception exception)
            {
                throw new IOException($"Write/Read test failed for directory path '{_directoryPath}'", exception);
            }
            finally
            {
                try
                {
                    File.Delete(filePath);
                }
                catch { }
            }
        }
    }
}