// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
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
        readonly string sagaIndexTableName;
        readonly string sagaTableName;

        public SqlServerSagaPersister(string connectionString, string sagaIndexTableName, string sagaTableName)
        {
            this.connectionString = connectionString;
            this.sagaIndexTableName = sagaIndexTableName;
            this.sagaTableName = sagaTableName;
        }

        public string SagaIndexTableName
        {
            get { return sagaIndexTableName; }
        }

        public string SagaTableName
        {
            get { return sagaTableName; }
        }

        public void Save(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // first, delete existing index
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"delete from [{0}] where saga_id = @id;", sagaIndexTableName);
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.ExecuteNonQuery();
                }

                // next, update the saga
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"update [{0}] set data = @data where id = @id", sagaTableName);
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.Parameters.AddWithValue("data", JsonConvert.SerializeObject(sagaData, Formatting.Indented, Settings));

                    var rows = command.ExecuteNonQuery();

                    if (rows == 0)
                    {
                        command.CommandText = string.Format(@"insert into [{0}] (id, data) values (@id, @data)", sagaTableName);
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
                            .Select(a => string.Format(
                                @"                      insert into [{0}]
                                                            (id, [key], value, saga_id) 
                                                        values 
                                                            ('{1}', '{2}', '{3}', '{4}')",
                                sagaIndexTableName,
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

        public ISagaData Find(string sagaDataPropertyPath, object fieldFromMessage, Type sagaDataType)
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
                    command.Parameters.AddWithValue("value", (fieldFromMessage ?? "").ToString());

                    return (ISagaData)JsonConvert.DeserializeObject((string)command.ExecuteScalar(), Settings);
                }
            }
        }
    }
}