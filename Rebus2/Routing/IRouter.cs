namespace Rebus2.Routing
{
    public interface IRouter
    {
        /// <summary>
        /// Called when sending messages
        /// </summary>
        string GetDestinationAddress(object message);

        /// <summary>
        /// Called when subscribing to messages
        /// </summary>
        string GetOwnerAddress(object message);
    }
}