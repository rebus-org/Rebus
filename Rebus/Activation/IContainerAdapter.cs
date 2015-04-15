using Rebus.Bus;

namespace Rebus.Activation
{
    public interface IContainerAdapter : IHandlerActivator
    {
        void SetBus(IBus bus);
    }
}