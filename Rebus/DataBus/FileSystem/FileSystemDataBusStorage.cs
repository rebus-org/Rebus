using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;

namespace Rebus.DataBus.FileSystem
{
    /// <summary>
    /// Implementation of <see cref="IDataBusStorage"/> that stores data in the file system. Could be a directory on a network share.
    /// </summary>
    public class FileSystemDataBusStorage : IDataBusStorage, IInitializable
    {
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
        public async Task Save(string id, Stream source)
        {
            var filePath = GetFilePath(id);

            using (var destination = File.Create(filePath))
            {
                await source.CopyToAsync(destination);
            }
        }

        /// <summary>
        /// Reads the data with the given ID and returns it as a stream
        /// </summary>
        public Stream Read(string id)
        {
            var filePath = GetFilePath(id);

            try
            {
                return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (FileNotFoundException exception)
            {
                throw new ArgumentException($"Could not find file for data with ID {id}", exception);
            }
        }

        string GetFilePath(string id)
        {
            return Path.Combine(_directoryPath, $"data-{id}.dat");
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