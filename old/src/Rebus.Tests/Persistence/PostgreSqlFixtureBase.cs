using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Config;
using Npgsql;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.PostgreSql;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Persistence
{
    public abstract class PostgreSqlFixtureBase : IDetermineMessageOwnership
    {
        protected const string SagaTableName = "testSagaTable";
        protected const string SagaIndexTableName = "testSagaIndexTable";

        const string ErrorQueueName = "error";

        static PostgreSqlFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        public static string ConnectionString
        {
            get { return ConnectionStrings.PostgreSql; }
        }

        public static void ExecuteCommand(string commandText)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
        }

        public static object ExecuteScalar(string commandText)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = commandText;
                    return command.ExecuteScalar();
                }
            }
        }

        public static void DropTable(string tableName)
        {
            if (!GetTableNames().Contains(tableName, StringComparer.InvariantCultureIgnoreCase)) return;

            ExecuteCommand(string.Format(@"DROP TABLE ""{0}""", tableName));
        }

        public static void DeleteRows(string tableName)
        {
            if (!GetTableNames().Contains(tableName, StringComparer.InvariantCultureIgnoreCase)) return;

            ExecuteCommand(string.Format(@"DELETE FROM ""{0}""", tableName));
        }

        public static List<string> GetTableNames()
        {
            var tableNames = new List<string>();

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT * FROM ""information_schema"".""tables"" WHERE ""table_schema"" NOT IN ('pg_catalog', 'information_schema')";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tableNames.Add(reader["table_name"].ToString());
                        }
                    }
                }
            }

            return tableNames;
        }

        public string GetEndpointFor(Type messageType)
        {
            return null;
        }

        [SetUp]
        public void SetUp()
        {
            TimeMachine.Reset();

            DoSetUp();
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();
            CleanUpTrackedDisposables();
        }

        protected static void DropSagaTables()
        {
            try
            {
                DropTable(SagaTableName);
            }
            catch
            {
            }

            try
            {
                DropTable(SagaIndexTableName);
            }
            catch
            {
            }
        }

        protected virtual void DoSetUp()
        {
        }

        protected virtual void DoTearDown()
        {
        }

        protected IStartableBus CreateBus(BuiltinContainerAdapter adapter, string inputQueueName)
        {
            var bus = Configure.With(adapter)
                               .Transport(t => t.UseMsmq(inputQueueName, ErrorQueueName))
                               .MessageOwnership(d => d.Use(this))
                               .Behavior(b => b.HandleMessagesInsideTransactionScope())
                               .Subscriptions(
                                   s =>
                                   s.StoreInPostgreSql(ConnectionString, "RebusSubscriptions")
                                    .EnsureTableIsCreated())
                               .Sagas(
                                   s =>
                                   s.StoreInPostgreSql(ConnectionString, SagaTableName, SagaIndexTableName)
                                    .EnsureTablesAreCreated())
                               .CreateBus();
            return bus;
        }

        protected T TrackDisposable<T>(T disposable) where T : IDisposable
        {
            DisposableTracker.TrackDisposable(disposable);
            return disposable;
        }

        protected void CleanUpTrackedDisposables()
        {
            DisposableTracker.DisposeTheDisposables();
        }
    }
}