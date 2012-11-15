using System;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Newtonsoft.Json;
using Ponder;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implements a saga persister for Rebus that stores sagas as a JSON serialized object in one table
    /// and correlation properties in an index table on the side.
    /// </summary>
    public class SqlServerSagaPersister : IStoreSagaData
    {
        const int PrimaryKeyViolationNumber = 2627;
        const int MaximumSagaDataTypeNameLength = 40;

        static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        readonly string sagaIndexTableName;
        readonly string sagaTableName;

        readonly Func<SqlConnection> getConnection;
        readonly Action<SqlConnection> releaseConnection;
        readonly string idPropertyName;

        SqlServerSagaPersister(string sagaIndexTableName, string sagaTableName)
        {
            this.sagaIndexTableName = sagaIndexTableName;
            this.sagaTableName = sagaTableName;
            idPropertyName = Reflect.Path<ISagaData>(d => d.Id);
        }

        /// <summary>
        /// Constructs the persister with the ability to create connections to SQL Server using the specified connection string.
        /// This also means that the persister will manage the connection by itself, closing it when it has stopped using it.
        /// </summary>
        public SqlServerSagaPersister(string connectionString, string sagaIndexTableName, string sagaTableName)
            : this(sagaIndexTableName, sagaTableName)
        {
            getConnection = () =>
                {
                    var sqlConnection = new SqlConnection(connectionString);
                    sqlConnection.Open();
                    return sqlConnection;
                };
            releaseConnection = c => c.Dispose();
        }

        /// <summary>
        /// Constructs the persister with the ability to use an externally provided <see cref="SqlConnection"/>, thus allowing it
        /// to easily enlist in any ongoing SQL transaction magic that might be going on. This means that the perister will assume
        /// that someone else manages the connection's lifetime.
        /// </summary>
        public SqlServerSagaPersister(Func<SqlConnection> connectionFactoryMethod, string sagaIndexTableName, string sagaTableName)
            : this(sagaIndexTableName, sagaTableName)
        {
            getConnection = connectionFactoryMethod;
            releaseConnection = c => { };
        }

        /// <summary>
        /// Returns the name of the table used to store correlation properties of saga instances
        /// </summary>
        public string SagaIndexTableName
        {
            get { return sagaIndexTableName; }
        }

        /// <summary>
        /// Returns the name of the table used to store JSON serializations of saga instances.
        /// </summary>
        public string SagaTableName
        {
            get { return sagaTableName; }
        }

        /// <summary>
        /// Inserts the given saga data in the underlying SQL table, generating an appropriate index in the index table for the specified
        /// correlation properties. In this process, all existing index entries associated with this particular saga data are deleted.
        /// </summary>
        public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var connection = getConnection();
            try
            {
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
                    .Where(a => !string.IsNullOrEmpty(a.Value))
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
            finally
            {
                releaseConnection(connection);
            }
        }

        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var connection = getConnection();
            try
            {
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
            finally
            {
                releaseConnection(connection);
            }
        }

        public void Delete(ISagaData sagaData)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"delete from [{0}] where id = @id and revision = @current_revision;", sagaTableName);
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
                    command.CommandText = string.Format(@"delete from [{0}] where saga_id = @id", sagaIndexTableName);
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    if (sagaDataPropertyPath == idPropertyName)
                    {
                        command.CommandText = string.Format(@"select s.data from [{0}] s where s.id = @value", sagaTableName);
                    }
                    else
                    {
                        command.CommandText = string.Format(@"select s.data 
                                                    from [{0}] s 
                                                        join [{1}] i on s.id = i.saga_id 
                                                    where i.[saga_type] = @saga_type
                                                        and i.[key] = @key 
                                                        and i.value = @value", sagaTableName, sagaIndexTableName);

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
            finally
            {
                releaseConnection(connection);
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

        /// <summary>
        /// Creates the necessary saga storage tables if they haven't already been created. If a table already exists
        /// with a name that matches one of the desired table names, no action is performed (i.e. it is assumed that
        /// the tables already exist).
        /// </summary>
        public SqlServerSagaPersister EnsureTablesAreCreated()
        {
            var connection = getConnection();
            try
            {
                using (var tx = new TransactionScope())
                {
                    var tableNames = connection.GetTableNames();

                    // bail out if there's already a table in the database with one of the names
                    if (tableNames.Contains(SagaTableName, StringComparer.InvariantCultureIgnoreCase)
                        || tableNames.Contains(SagaIndexTableName, StringComparer.OrdinalIgnoreCase))
                    {
                        return this;
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}](
	[id] [uniqueidentifier] NOT NULL,
	[revision] [int] NOT NULL,
	[data] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

", SagaTableName);
                        command.ExecuteNonQuery();
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}](
	[saga_type] [nvarchar](40) NOT NULL,
	[key] [nvarchar](200) NOT NULL,
	[value] [nvarchar](200) NOT NULL,
	[saga_id] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
(
	[key] ASC,
	[value] ASC,
	[saga_type] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

", SagaIndexTableName);
                        command.ExecuteNonQuery();
                    }

                    tx.Complete();
                }
            }
            finally
            {
                releaseConnection(connection);
            }
            return this;
        }
    }
}