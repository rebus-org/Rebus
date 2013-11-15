using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using NUnit.Framework;
using log4net.Config;
using System.Linq;

namespace Rebus.Tests.Persistence
{
    public class SqlServerFixtureBase
    {
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

        protected void DropTable(string tableName)
        {
            if (!GetTableNames()
                     .Contains(tableName, StringComparer.InvariantCultureIgnoreCase)) return;

            ExecuteCommand("drop table " + tableName);
        }

        protected void DeleteRows(string tableName)
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