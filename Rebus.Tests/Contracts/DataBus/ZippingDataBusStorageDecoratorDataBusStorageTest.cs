using NUnit.Framework;
using Rebus.Tests.Contracts.DataBus.Factories;

namespace Rebus.Tests.Contracts.DataBus
{
    [TestFixture]
    public class ZippingDataBusStorageDecoratorDataBusStorageTest : GeneralDataBusStorageTests<ZippingDataBusStorageDecoratorFactory> { }
}