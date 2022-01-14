using Rebus.DataBus;

namespace Rebus.Testing;

static class TestBackdoor
{
    public static void EnableTestMode(IDataBusStorage dataBusStorage)
    {
        TestDataBusStorage = dataBusStorage;
    }

    public static void Reset()
    {
        EnableTestMode(null);
    }

    public static IDataBusStorage TestDataBusStorage { get; private set; }
}