using NUnit.Framework;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.Tests.DataBus.FileSystem;

[TestFixture]
public class FileSystemDataBusStorageTest : GeneralDataBusStorageTests<FileSystemDataBusStorageFactory> { }