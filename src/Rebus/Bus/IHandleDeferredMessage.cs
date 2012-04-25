namespace Rebus.Bus
{
    interface IHandleDeferredMessage
    {
        void Dispatch(object deferredMessage);
    }
}