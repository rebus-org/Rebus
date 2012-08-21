using Rebus.Persistence.InMemory;
using Rebus.Persistence.SqlServer;

namespace Rebus.Configuration
{
    public class RebusSagasConfigurer
    {
        readonly ConfigurationBackbone backbone;

        public RebusSagasConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }

        public void Use(IStoreSagaData storeSagaData)
        {
            backbone.StoreSagaData = storeSagaData;
        }

        public void StoreInSqlServer(string connectionstring, string sagaTable, string sagaIndexTable)
        {
            Use(new SqlServerSagaPersister(connectionstring, sagaIndexTable, sagaTable));
        }

        public void StoreInMemory()
        {
            Use(new InMemorySagaPersister());
        }
    }
}