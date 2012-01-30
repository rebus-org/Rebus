namespace Rebus.Transports
{
    public interface IHavePurgableInputQueue<T>
    {
        T PurgeInputQueue();
    }
}