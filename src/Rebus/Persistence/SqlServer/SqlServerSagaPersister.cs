using System;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using Ponder;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerSagaPersister : IStoreSagaData
    {
        static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        readonly string connectionString;

        public SqlServerSagaPersister(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Save(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            using(var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // first, delete all existing indexes
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"delete from saga_index where saga_id = @saga_id";
                    command.Parameters.AddWithValue("saga_id", sagaData.Id);
                    command.ExecuteNonQuery();
                }

                // next, update the saga
                using(var command = connection.CreateCommand())
                {
                    command.CommandText = @"insert into sagas (id, data) values (@id, @data)";
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.Parameters.AddWithValue("data", JsonConvert.SerializeObject(sagaData, Formatting.Indented, Settings));
                    
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch(SqlException e)
                    {
                        if (e.Number == 2627)
                        {
                            command.CommandText = @"update sagas set data = @data where id = @id";
                            command.ExecuteNonQuery();
                        }
                        else throw;
                    }
                }

                if (sagaDataPropertyPathsToIndex.Length > 0)
                {
                    // lastly, generate new index
                    using (var command = connection.CreateCommand())
                    {
                        // generate batch insert with SQL for each entry in the index
                        var inserts = sagaDataPropertyPathsToIndex
                            .Select(path => new
                                                {
                                                    Key = path,
                                                    Value = (Reflect.Value(sagaData, path) ?? "").ToString()
                                                })
                            .Select(a => string.Format(@"insert into saga_index ([id], [key], [value], [saga_id]) values ('{0}', '{1}', '{2}', '{3}')",
                                                       Guid.NewGuid(), a.Key, a.Value,
                                                       sagaData.Id.ToString()));
                        
                        var sql = string.Join(";" + Environment.NewLine, inserts);
                        
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void Delete(ISagaData sagaData)
        {
        }

        public ISagaData Find(string sagaDataPropertyPath, string fieldFromMessage)
        {
            return null;
        }
    }
}