using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Persistence.SqlServer;

namespace Rebus.Tests.Transport.SqlServer
{
    [TestFixture, Category(Categories.SqlServer)]
    public class TestDbConnectionProvider
    {
        [Test, Ignore("assumes existence of a bimse table")]
        public async Task CanDoWorkInTransaction()
        {
            var provizzle = new DbConnectionProvider(SqlTestHelper.ConnectionString);

            using (var dbConnection = await provizzle.GetConnection())
            {
                using (var cmd = dbConnection.CreateCommand())
                {
                    cmd.CommandText = "insert into bimse (text) values ('hej med dig')";
                    
                    await cmd.ExecuteNonQueryAsync();
                }

                //await dbConnection.Complete();
            }
        }
    }
}