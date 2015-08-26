using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.Tests.Transport.FileSystem
{
    [TestFixture, Category(Categories.Msmq)]
    public class FileSystemTransportBasicSendReceive : BasicSendReceive<FileSystemTransportFactory> { }
}