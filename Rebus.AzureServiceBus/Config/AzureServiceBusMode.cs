namespace Rebus.AzureServiceBus.Config
{
    /// <summary>
    /// Represents the two supported modes of using Azure Service Bus
    /// </summary>
    public enum AzureServiceBusMode
    {
        /// <summary>
        /// In standard mode, the Azure Service Bus transport will use ASB topics to do pub/sub
        /// </summary>
        Standard,

        /// <summary>
        /// In basic mode, only ASB queues are necessary
        /// </summary>
        Basic
    }
}