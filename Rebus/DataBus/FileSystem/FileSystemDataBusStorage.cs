using System;
using System.IO;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;

namespace Rebus.DataBus.FileSystem
{
    public class FileSystemDataBusStorage : IDataBusStorage, IInitializable
    {
        readonly string _directoryPath;
        readonly ILog _log;

        public FileSystemDataBusStorage(string directoryPath, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (directoryPath == null) throw new ArgumentNullException(nameof(directoryPath));
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _directoryPath = directoryPath;
            _log = rebusLoggerFactory.GetCurrentClassLogger();
        }

        public void Initialize()
        {
            if (!Directory.Exists(_directoryPath))
            {
                _log.Info("Creating directory {0}", _directoryPath);
                Directory.CreateDirectory(_directoryPath);
            }
        }

        public Task Save(string id, Stream source)
        {
            throw new System.NotImplementedException();
        }

        public Stream Read(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}