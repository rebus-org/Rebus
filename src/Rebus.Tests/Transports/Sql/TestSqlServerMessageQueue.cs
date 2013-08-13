using System.Data.SqlClient;
using System.Transactions;
using NUnit.Framework;
using Rebus.Tests.Persistence;
using Rebus.Persistence.SqlServer;

namespace Rebus.Tests.Transports.Sql
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerMessageQueue : SqlServerFixtureBase
    {
        [Test, Ignore("Was only used to demonstrate that the approach taken in SqlServerMessageQueue was viable")]
        public void CanDoItManually()
        {
            string id = null;

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                connection.BeginTransaction();

                using (var c = connection.CreateCommand())
                {
                    connection.AssignTransactionIfNecessary(c);
                    c.CommandText = "delete from messages";
                    c.ExecuteNonQuery();
                }

                connection.GetTransactionOrNull()
                          .Commit();
            }

            using (new TransactionScope())
            {
                var tx2 = new TransactionScope(TransactionScopeOption.Suppress);
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    connection.BeginTransaction();

                    tx2.Dispose();

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "insert into messages (id, data) values (@id, @data)";
                        c.Parameters.AddWithValue("id", "1");
                        c.Parameters.AddWithValue("data", "hej1");
                        c.ExecuteNonQuery();
                    }

                    connection.GetTransactionOrNull()
                              .Commit();
                }

                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    connection.BeginTransaction();

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "select top 1 id, data from messages with (updlock, readpast)";
                        using (var reader = c.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                id = (string)reader["id"];
                            }
                        }
                    }

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "insert into messages (id, data) values (@id, @data)";
                        c.Parameters.AddWithValue("id", "2");
                        c.Parameters.AddWithValue("data", "hej2");
                        c.ExecuteNonQuery();
                    }

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "delete from messages where id = @id";
                        c.Parameters.AddWithValue("id", id);
                        c.ExecuteNonQuery();
                    }

                    using (var c = connection.CreateCommand())
                    {
                        connection.AssignTransactionIfNecessary(c);
                        c.CommandText = "insert into messages (id, data) values (@id, @data)";
                        c.Parameters.AddWithValue("id", "3");
                        c.Parameters.AddWithValue("data", "hej3");
                        c.ExecuteNonQuery();
                    }

                    connection.GetTransactionOrNull()
                              .Rollback();
                }
            }
        }
    }
}