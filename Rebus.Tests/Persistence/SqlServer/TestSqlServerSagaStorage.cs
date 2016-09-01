using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Sagas;
using Rebus.Tests.Contracts;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture]
    public class TestSqlServerSagaStorage : FixtureBase
    {
        SqlServerSagaStorage _storage;
        string _dataTableName;
        DbConnectionProvider _connectionProvider;
        string _indexTableName;

        protected override void SetUp()
        {
            var loggerFactory = new ConsoleLoggerFactory(false);
            _connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, loggerFactory);

            _dataTableName = TestConfig.GetName("sagas");
            _indexTableName = TestConfig.GetName("sagaindex");

            SqlTestHelper.DropTable(_indexTableName);
            SqlTestHelper.DropTable(_dataTableName);

            _storage = new SqlServerSagaStorage(_connectionProvider, _dataTableName, _indexTableName, loggerFactory);
        }

        [Test]
        public async Task DoesNotThrowExceptionWhenInitializeOnOldSchema()
        {
            await CreatePreviousSchema();

            _storage.Initialize();

            _storage.EnsureTablesAreCreated();
        }

        [Test]
        public async Task CanRoundtripSagaOnOldSchema()
        {
            var noProps = Enumerable.Empty<ISagaCorrelationProperty>();

            await CreatePreviousSchema();

            _storage.Initialize();

            var sagaData = new MySagaDizzle {Id=Guid.NewGuid(), Text = "whee!"};

            await _storage.Insert(sagaData, noProps);

            var roundtrippedData = await _storage.Find(typeof(MySagaDizzle), "Id", sagaData.Id.ToString());

            Assert.That(roundtrippedData, Is.TypeOf<MySagaDizzle>());
            var sagaData2 = (MySagaDizzle)roundtrippedData;
            Assert.That(sagaData2.Text, Is.EqualTo(sagaData.Text));
        }

        class MySagaDizzle : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string Text { get; set; }
        }

        async Task CreatePreviousSchema()
        {
            var createTableOldSchema =
                $@"

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

            var createTableOldSchema2 =
                $@"

CREATE TABLE [dbo].[{_indexTableName}](
	[saga_type] [nvarchar](40) NOT NULL,
	[key] [nvarchar](200) NOT NULL,
	[value] [nvarchar](200) NOT NULL,
	[saga_id] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_{_indexTableName}] PRIMARY KEY CLUSTERED 
 (
	[key] ASC,
	[value] ASC,
	[saga_type] ASC
 ))
";

            Console.WriteLine($"Creating tables {_dataTableName} and {_indexTableName}");

            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createTableOldSchema;
                    command.ExecuteNonQuery();
                }
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createTableOldSchema2;
                    command.ExecuteNonQuery();
                }

                await connection.Complete();
            }
        }
    }
}