namespace Rebus.Bus
{
    /// <summary>
    /// Enumeration of the different bus modes
    /// </summary>
    enum BusMode
    {
        /// <summary>
        /// The ordinary full duplex bus mode
        /// </summary>
        Unspecified,

        /// <summary>
        /// One-way mode that is capable only of sending messages
        /// </summary>
        OneWayClientMode,
    }
}