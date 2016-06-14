using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture]
    public class TestSqlServerSagaStorage : FixtureBase
    {
        SqlServerSagaStorage _storage;
        string _dataTableName;
        DbConnectionProvider _connectionProvider;

        protected override void SetUp()
        {
            var loggerFactory = new ConsoleLoggerFactory(false);
            _connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, loggerFactory);

            _dataTableName = TestConfig.QueueName("sagas");
            var indexTableName = TestConfig.QueueName("sagaindex");

            SqlTestHelper.DropTable(indexTableName);
            SqlTestHelper.DropTable(_dataTableName);

            _storage = new SqlServerSagaStorage(_connectionProvider, _dataTableName, indexTableName, loggerFactory);
        }

        [Test]
        public async Task ThrowsExceptionWhenInitializeOnOldSchema()
        {
            var createTableOldSchema = $@"

CREATE TABLE [dbo].[{_dataTableName}](
	[id] [uniqueidentifier] NOT NULL,
	[revision] [int] NOT NULL,
	[data] [nvarchar](max) NOT NULL,
	 CONSTRAINT [PK_{_dataTableName}] PRIMARY KEY CLUSTERED 
	(
		[id] ASC
	)
)

";

            Console.WriteLine($"Creating table {_dataTableName}");

            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createTableOldSchema;
                    command.ExecuteNonQuery();
                }

                await connection.Complete();
            }

            Console.WriteLine("Telling saga storage to create its schema");

            var exception = Assert.Throws<AggregateException>(() =>
            {
                _storage.EnsureTablesAreCreated();
            });

            Console.WriteLine(exception);
        }
    }
}