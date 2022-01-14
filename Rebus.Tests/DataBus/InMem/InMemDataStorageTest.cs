using NUnit.Framework;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.Tests.DataBus.InMem;

[TestFixture]
public class InMemDataBusStorageTest : GeneralDataBusStorageTests<InMemDataBusStorageFactory> { }