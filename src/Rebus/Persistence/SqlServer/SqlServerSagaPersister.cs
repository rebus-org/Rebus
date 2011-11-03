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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // first, delete existing index
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"delete from saga_index where saga_id = @id;";
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.ExecuteNonQuery();
                }

                // next, update the saga
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"update sagas set data = @data where id = @id";
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.Parameters.AddWithValue("data", JsonConvert.SerializeObject(sagaData, Formatting.Indented, Settings));

                    var rows = command.ExecuteNonQuery();

                    if (rows == 0)
                    {
                        command.CommandText = @"insert into sagas (id, data) values (@id, @data)";
                        command.ExecuteNonQuery();
                    }
                }

                var propertiesToIndex = sagaDataPropertyPathsToIndex
                    .Select(path => new
                                        {
                                            Key = path,
                                            Value = (Reflect.Value(sagaData, path) ?? "").ToString()
                                        })
                    .Where(a => a.Value != null)
                    .ToList();

                if (propertiesToIndex.Any())
                {
                    // lastly, generate new index
                    using (var command = connection.CreateCommand())
                    {
                        // generate batch insert with SQL for each entry in the index
                        var inserts = propertiesToIndex
                            .Select(a => string.Format(@"insert into saga_index
                                                            (id, [key], value, saga_id) 
                                                        values 
                                                            ('{0}', '{1}', '{2}', '{3}')",
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"delete from sagas where id = @id;
                                            delete from saga_index where saga_id = @id";
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public ISagaData Find(string sagaDataPropertyPath, string fieldFromMessage, Type sagaDataType)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"select s.data 
                                                from sagas s 
                                                    join saga_index i on s.id = i.saga_id 
                                                where i.[key] = @key 
                                                    and i.value = @value";
                    command.Parameters.AddWithValue("key", sagaDataPropertyPath);
                    command.Parameters.AddWithValue("value", fieldFromMessage);

                    return (ISagaData)JsonConvert.DeserializeObject((string)command.ExecuteScalar(), Settings);
                }
            }
        }
    }
}