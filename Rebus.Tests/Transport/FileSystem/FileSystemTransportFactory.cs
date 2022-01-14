using System;
using System.Collections.Concurrent;
using System.IO;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Transports;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Time;
using Rebus.Transport;
using Rebus.Transport.FileSystem;

namespace Rebus.Tests.Transport.FileSystem;

public class FileSystemTransportFactory : ITransportFactory
{
    readonly ConcurrentStack<IDisposable> _disposables = new ConcurrentStack<IDisposable>();
    readonly string _baseDirectory;

    public FileSystemTransportFactory()
    {
        _baseDirectory = Path.Combine(TestConfig.DirectoryPath(), "messages");

        CleanUp();
    }

    public ITransport CreateOneWayClient()
    {
        return new FileSystemTransport(_baseDirectory, null, new FileSystemTransportOptions(), new FakeRebusTime());
    }

    public ITransport Create(string inputQueueAddress)
    {
        var fileSystemTransport = new FileSystemTransport(_baseDirectory, inputQueueAddress, new FileSystemTransportOptions(), new FakeRebusTime());
        _disposables.Push(fileSystemTransport);
        fileSystemTransport.Initialize();;
        return fileSystemTransport;
    }

    public void CleanUp()
    {
        while (_disposables.TryPop(out var disposable))
        {
            disposable.Dispose();
        }
        DeleteHelper.DeleteDirectory(_baseDirectory);
    }
}