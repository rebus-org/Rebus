using Rebus.Configuration;

namespace Rebus.IdempotentSagas
{
    public static class IdempotentSagaExtensions
    {
        public static RebusSagasConfigurer WithIdempotentSagas(this RebusSagasConfigurer configurer)
        {
            var manager = new IdempotentSagasManager(configurer.Backbone);
            return configurer;
        }
    }
}
