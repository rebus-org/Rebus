namespace Rebus.Configuration.Configurers
{
    public class TransportConfigurer
    {
        readonly IContainerAdapter containerAdapter;

        public TransportConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public void Use<T>(T instance) where T : ISendMessages, IReceiveMessages
        {
            containerAdapter.RegisterInstance(instance, typeof(ISendMessages), typeof(IReceiveMessages));
        }
    }
}