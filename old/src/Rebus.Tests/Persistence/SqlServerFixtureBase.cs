using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Transports.Msmq;
using log4net.Config;
using System.Linq;

namespace Rebus.Tests.Persistence
{
    public abstract class SqlServerFixtureBase:IDetermineMessageOwnership
    {
        protected const string SagaTableName = "testSagaTable";
        protected const string SagaIndexTableName = "testSagaIndexTable";

        const string ErrorQueueName = "error";
        
        static SqlServerFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        public static string ConnectionString
        {
            get { return ConnectionStrings.SqlServer; }
        }

        [SetUp]
        public void SetUp()
        {
            TimeMachine.Reset();
            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();
            CleanUpTrackedDisposables();
        }

        protected virtual void DoTearDown()
        {
        }

        public static void DropTable(string tableName)
        {
            if (!GetTableNames()
                     .Contains(tableName, StringComparer.InvariantCultureIgnoreCase)) return;

            ExecuteCommand("drop table " + tableName);
        }

        public static void DeleteRows(string tableName)
        {
            if (!GetTableNames()
                     .Contains(tableName, StringComparer.InvariantCultureIgnoreCase)) return;

            ExecuteCommand("delete from " + tableName);
        }

        public static List<string> GetTableNames()
        {
            var tableNames = new List<string>();
            using(var conn = new SqlConnection(ConnectionStrings.SqlServer))
            {
                conn.Open();

                using(var command = conn.CreateCommand())
                {
                    command.CommandText = "select * from sys.Tables";
                    
                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            tableNames.Add(reader["name"].ToString());
                        }
                    }
                }
            }
            return tableNames;
        }

        public static void ExecuteCommand(string commandText)
        {
            using (var conn = new SqlConnection(ConnectionStrings.SqlServer))
            {
                conn.Open();

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
        }

        public static object ExecuteScalar(string commandText)
        {
            using (var conn = new SqlConnection(ConnectionStrings.SqlServer))
            {
                conn.Open();

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = commandText;
                    return command.ExecuteScalar();
                }
            }
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

        protected static void DropSagaTables()
        {
            try { ExecuteCommand("drop table " + SagaTableName); }
            catch { }
            try { ExecuteCommand("drop table " + SagaIndexTableName); }
            catch { }
        }

        protected IStartableBus CreateBus(BuiltinContainerAdapter adapter, string inputQueueName)
        {
            var bus = Configure.With(adapter)
                               .Transport(t => t.UseMsmq(inputQueueName, ErrorQueueName))
                               .MessageOwnership(d => d.Use(this))
                               .Behavior(b => b.HandleMessagesInsideTransactionScope())
                               .Subscriptions(
                                   s =>
                                   s.StoreInSqlServer(ConnectionString, "RebusSubscriptions")
                                    .EnsureTableIsCreated())
                               .Sagas(
                                   s =>
                                   s.StoreInSqlServer(ConnectionString, SagaTableName, SagaIndexTableName)
                                    .EnsureTablesAreCreated())
                               .CreateBus();
            return bus;
        }

        public virtual string GetEndpointFor(Type messageType)
        {
            return null;
        }
    }
}