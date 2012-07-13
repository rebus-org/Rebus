using System;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using Ponder;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerSagaPersister : IStoreSagaData
    {
        const int PrimaryKeyViolationNumber = 2627;
        const int MaximumSagaDataTypeNameLength = 40;

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

        public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // next insert the saga
                using (var command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.Parameters.AddWithValue("current_revision", sagaData.Revision);

                    sagaData.Revision++;
                    command.Parameters.AddWithValue("next_revision", sagaData.Revision);
                    command.Parameters.AddWithValue("data", JsonConvert.SerializeObject(sagaData, Formatting.Indented, Settings));

                    command.CommandText = string.Format(@"insert into [{0}] (id, revision, data) values (@id, @next_revision, @data)", sagaTableName);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException)
                    {
                        throw new OptimisticLockingException(sagaData);
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
                                                            ([saga_type], [key], value, saga_id) 
                                                        values 
                                                            ('{1}', '{2}', '{3}', '{4}')",
                                sagaIndexTableName, GetSagaTypeName(sagaData.GetType()), a.Key, a.Value,
                                sagaData.Id.ToString()));

                        var sql = string.Join(";" + Environment.NewLine, inserts);

                        command.CommandText = sql;

                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch(SqlException sqlException)
                        {
                            if (sqlException.Number == PrimaryKeyViolationNumber)
                            {
                                throw new OptimisticLockingException(sagaData, sqlException);
                            }

                            throw;
                        }
                    }
                }
            }
        }

        string GetSagaTypeName(Type sagaDataType)
        {
            var sagaTypeName = sagaDataType.Name;

            if (sagaTypeName.Length > MaximumSagaDataTypeNameLength)
            {
                throw new InvalidOperationException(
                    string.Format(
                        @"Sorry, but the maximum length of the name of a saga data class is currently limited to {0} characters!

This is due to a limitation in SQL Server, where compound indexes have a 900 byte upper size limit - and
since the saga index needs to be able to efficiently query by saga type, key, and value at the same time,
there's room for only 200 characters as the key, 200 characters as the value, and 40 characters as the
saga type name.",
                        MaximumSagaDataTypeNameLength));
            }

            return sagaTypeName;
        }

        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
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

                // next, update or insert the saga
                using (var command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.Parameters.AddWithValue("current_revision", sagaData.Revision);

                    sagaData.Revision++;
                    command.Parameters.AddWithValue("next_revision", sagaData.Revision);
                    command.Parameters.AddWithValue("data", JsonConvert.SerializeObject(sagaData, Formatting.Indented, Settings));

                    command.CommandText = string.Format(@"update [{0}] set data = @data, revision = @next_revision where id = @id and revision = @current_revision", sagaTableName);
                    var rows = command.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        throw new OptimisticLockingException(sagaData);
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
                                                            ([saga_type], [key], value, saga_id) 
                                                        values 
                                                            ('{1}', '{2}', '{3}', '{4}')",
                                sagaIndexTableName, GetSagaTypeName(sagaData.GetType()), a.Key, a.Value,
                                sagaData.Id.ToString()));

                        var sql = string.Join(";" + Environment.NewLine, inserts);

                        command.CommandText = sql;
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (SqlException sqlException)
                        {
                            if (sqlException.Number == PrimaryKeyViolationNumber)
                            {
                                throw new OptimisticLockingException(sagaData, sqlException);
                            }

                            throw;
                        }
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
                    command.CommandText = @"delete from sagas where id = @id and revision = @current_revision;";
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.Parameters.AddWithValue("current_revision", sagaData.Revision);
                    var rows = command.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        throw new OptimisticLockingException(sagaData);
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"delete from saga_index where saga_id = @id";
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    if (sagaDataPropertyPath == "Id")
                    {
                        command.CommandText = @"select s.data from sagas s where s.id = @value";
                    }
                    else
                    {
                        command.CommandText = @"select s.data 
                                                    from sagas s 
                                                        join saga_index i on s.id = i.saga_id 
                                                    where i.[saga_type] = @saga_type
                                                        and i.[key] = @key 
                                                        and i.value = @value";
                        command.Parameters.AddWithValue("key", sagaDataPropertyPath);
                        command.Parameters.AddWithValue("saga_type", GetSagaTypeName(typeof(T)));
                    }

                    command.Parameters.AddWithValue("value", (fieldFromMessage ?? "").ToString());

                    var value = (string)command.ExecuteScalar();

                    if (value == null)
                        return null;

                    return (T)JsonConvert.DeserializeObject(value, Settings);
                }
            }
        }
    }
}