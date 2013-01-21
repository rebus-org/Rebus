namespace Rebus.RabbitMQ
{
    /// <summary>
    /// Used to establish a transient subscription with Rabbit. Will be called each time the Rabbit client
    /// reconnects.
    /// </summary>
    public interface ISubscribeTransient
    {
        void Subscribe<TEvent>();
    }
}