using System;
using System.IO;
using Rebus.Logging;
using Rebus.Persistence.FileSystem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Time;
using Rebus.Timeouts;

namespace Rebus.Tests.Persistence.Filesystem;

public class FilesystemTimeoutManagerFactory : ITimeoutManagerFactory
{
    readonly string _basePath = Path.Combine(TestConfig.DirectoryPath(), $"Timeouts{DateTime.Now:yyyyMMddHHmmssffff}");
    readonly FakeRebusTime _fakeRebusTime = new FakeRebusTime();

    public ITimeoutManager Create()
    {
        return new FileSystemTimeoutManager(_basePath, new ConsoleLoggerFactory(false), _fakeRebusTime);
    }

    public void Cleanup()
    {
        DeleteHelper.DeleteDirectory(_basePath);
    }

    public string GetDebugInfo()
    {
        return "could not provide debug info for this particular timeout manager.... implement if needed :)";
    }

    public void FakeIt(DateTimeOffset fakeTime)
    {
        _fakeRebusTime.FakeIt(fakeTime);
    }
}