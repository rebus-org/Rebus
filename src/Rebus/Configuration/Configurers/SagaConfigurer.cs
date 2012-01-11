namespace Rebus.Configuration.Configurers
{
    public class SagaConfigurer
    {
        readonly IContainerAdapter containerAdapter;

        public SagaConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public void Use<T>(T instance) where T : IStoreSagaData
        {
            containerAdapter.RegisterInstance(instance, typeof(IStoreSagaData));
        }
    }
}