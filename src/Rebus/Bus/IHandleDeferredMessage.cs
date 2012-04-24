namespace Rebus.Bus
{
    public interface IHandleDeferredMessage
    {
        void Dispatch(object deferredMessage);
    }
}