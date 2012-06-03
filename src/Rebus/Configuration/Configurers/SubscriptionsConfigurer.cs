namespace Rebus.Configuration.Configurers
{
    public class SubscriptionsConfigurer
    {
        readonly IContainerAdapter containerAdapter;

        public SubscriptionsConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public void Use<T>(T instance) where T : IStoreSubscriptions
        {
            containerAdapter.RegisterInstance(instance, typeof(IStoreSubscriptions));
        }
    }
}