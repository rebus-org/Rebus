using Rebus.Bus;

namespace Rebus.Configuration.Configurers
{
    public class TransportConfigurer
    {
        readonly IContainerAdapter containerAdapter;

        public TransportConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public void UseSender<T>(T instance) where T : ISendMessages
        {
            containerAdapter.RegisterInstance(instance, typeof(ISendMessages));
        }

        public void UseReceiver<T>(T instance) where T : IReceiveMessages
        {
            containerAdapter.RegisterInstance(instance, typeof(IReceiveMessages));
        }

        public void UseErrorTracker<T>(T instance) where T : IErrorTracker
        {
            containerAdapter.RegisterInstance(instance, typeof (IErrorTracker));
        }
    }
}