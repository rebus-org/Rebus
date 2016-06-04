using Rebus.Config;

namespace Rebus.Tests.Sagas.Locking
{
    public static class PessimisticSagaLockingConfigurationExtensions
    {
        public static PessimisticSagaLockingConfigurer EnablePessimisticSagaLocking(this OptionsConfigurer configurer)
        {
            return new PessimisticSagaLockingConfigurer(configurer);
        }
    }
}