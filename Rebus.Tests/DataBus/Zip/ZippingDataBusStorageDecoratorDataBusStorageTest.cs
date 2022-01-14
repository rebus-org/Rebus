using NUnit.Framework;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.Tests.DataBus.Zip;

[TestFixture]
public class ZippingDataBusStorageDecoratorDataBusStorageTest : GeneralDataBusStorageTests<ZippingDataBusStorageDecoratorFactory> { }