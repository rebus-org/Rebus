using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.Tests.Transport.FileSystem;

[TestFixture]
public class FileSystemTransportMessageExpiration : MessageExpiration<FileSystemTransportFactory> { }