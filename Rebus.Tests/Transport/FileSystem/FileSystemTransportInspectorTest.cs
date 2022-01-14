using System.IO;
using NUnit.Framework;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Transports;
using Rebus.Tests.Time;
using Rebus.Transport.FileSystem;

namespace Rebus.Tests.Transport.FileSystem;

[TestFixture]
public class FileSystemTransportInspectorTest : TransportInspectorTest<FileSystemTransportInspectorTest.FileSystemTransportInspectorFactory>
{
    public class FileSystemTransportInspectorFactory : ITransportInspectorFactory
    {
        string _path;

        public FileSystemTransportInspectorFactory()
        {
            _path = Path.Combine(TestConfig.DirectoryPath(), "transport");

            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, true);
            }

            Directory.CreateDirectory(_path);
        }

        public TransportAndInspector Create(string address)
        {
            var transport = new FileSystemTransport(_path, address, new FileSystemTransportOptions(), new FakeRebusTime());
            return new TransportAndInspector(transport, transport);
        }

        public void Dispose()
        {
        }
    }
}