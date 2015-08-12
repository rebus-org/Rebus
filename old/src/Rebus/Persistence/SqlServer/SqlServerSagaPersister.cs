using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using Ponder;
using Rebus.Logging;
using Rebus.Transports.Sql;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implements a saga persister for Rebus that stores sagas as a JSON serialized object in one table
    /// and correlation properties in an index table on the side.
    /// </summary>
    public class SqlServerSagaPersister : SqlServerStorage, IStoreSagaData, ICanUpdateMultipleSagaDatasAtomically
    {
        static ILog log;

        static SqlServerSagaPersister()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        const int MaximumSagaDataTypeNameLength = 40;

        static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        string sagaIndexTableName;
        string sagaTableName;

        string idPropertyName;
        bool indexNullProperties = true;

        /// <summary>
        /// Constructs the persister with the ability to create connections to SQL Server using the specified connection string.
        /// This also means that the persister will manage the connection by itself, closing it when it has stopped using it.
        /// </summary>
        public SqlServerSagaPersister(string connectionStringOrConnectionStringName, string sagaIndexTableName, string sagaTableName)
            : base(connectionStringOrConnectionStringName)
        {
            Initialize(sagaIndexTableName, sagaTableName);
        }

        void Initialize(string sagaIndexTableName, string sagaTableName)
        {
            this.sagaIndexTableName = sagaIndexTableName;
            this.sagaTableName = sagaTableName;
            idPropertyName = Reflect.Path<ISagaData>(d => d.Id);
        }

        /// <summary>
        /// Constructs the persister with the ability to use an externally provided <see cref="SqlConnection"/>, thus allowing it
        /// to easily enlist in any ongoing SQL transaction magic that might be going on. This means that the perister will assume
        /// that someone else manages the connection's lifetime.
        /// </summary>
        public SqlServerSagaPersister(Func<ConnectionHolder> connectionFactoryMethod, string sagaIndexTableName, string sagaTableName)
            : base(connectionFactoryMethod)
        {
            Initialize(sagaIndexTableName, sagaTableName);
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

                    command.CommandText = string.Format(@"insert into [{0}] ([id], [revision], [data]) values (@id, @next_revision, @data)", sagaTableName);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException sqlException)
                    {
                        if (sqlException.Number == SqlServerMagic.PrimaryKeyViolationNumber)
                        {
                            throw new OptimisticLockingException(sagaData, sqlException);
                        }

                        throw;
                    }
                }

                var propertiesToIndex = GetPropertiesToIndex(sagaData, sagaDataPropertyPathsToIndex);

                if (propertiesToIndex.Any())
                {
                    CreateIndex(propertiesToIndex, connection, sagaData);
                }

                commitAction(connection);
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        /// <summary>
        /// Updates the given saga data in the underlying SQL table, generating an appropriate index in the index table for the specified
        /// correlation properties. In this process, all existing index entries associated with this particular saga data are deleted.
        /// </summary>
        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var connection = getConnection();
            try
            {
                // first, delete existing index
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"delete from [{0}] where [saga_id] = @id;", sagaIndexTableName);
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

                    command.CommandText = string.Format(@"update [{0}] set [data] = @data, [revision] = @next_revision where [id] = @id and [revision] = @current_revision", sagaTableName);
                    var rows = command.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        throw new OptimisticLockingException(sagaData);
                    }
                }

                var propertiesToIndex = GetPropertiesToIndex(sagaData, sagaDataPropertyPathsToIndex);

                if (propertiesToIndex.Any())
                {
                    CreateIndex(propertiesToIndex, connection, sagaData);
                }

                commitAction(connection);
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        void CreateIndex(IEnumerable<KeyValuePair<string, string>> propertiesToIndex, ConnectionHolder connection, ISagaData sagaData)
        {
            var sagaTypeName = GetSagaTypeName(sagaData.GetType());
            var parameters = propertiesToIndex
                .Select((p, i) => new
                {
                    PropertyName = p.Key,
                    PropertyValue = p.Value ?? "",
                    PropertyNameParameter = string.Format("@n{0}", i),
                    PropertyValueParameter = string.Format("@v{0}", i)
                })
                .ToList();

            // lastly, generate new index
            using (var command = connection.CreateCommand())
            {
                // generate batch insert with SQL for each entry in the index
                var inserts = parameters
                    .Select(a => string.Format(
                        @"                      insert into [{0}]
                                                            ([saga_type], [key], [value], [saga_id]) 
                                                        values 
                                                            (@saga_type, {1}, {2}, @saga_id)",
                        sagaIndexTableName, a.PropertyNameParameter, a.PropertyValueParameter))
                    .ToList();

                var sql = string.Join(";" + Environment.NewLine, inserts);

                command.CommandText = sql;

                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter.PropertyNameParameter, SqlDbType.NVarChar).Value = parameter.PropertyName;
                    command.Parameters.Add(parameter.PropertyValueParameter, SqlDbType.NVarChar).Value = parameter.PropertyValue;
                }

                command.Parameters.Add("saga_type", SqlDbType.NVarChar).Value = sagaTypeName;
                command.Parameters.Add("saga_id", SqlDbType.UniqueIdentifier).Value = sagaData.Id;

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqlException sqlException)
                {
                    if (sqlException.Number == SqlServerMagic.PrimaryKeyViolationNumber)
                    {
                        throw new OptimisticLockingException(sagaData, sqlException);
                    }

                    throw;
                }
            }

        }

        /// <summary>
        /// Deletes the saga data instance from the underlying SQL table
        /// </summary>
        public void Delete(ISagaData sagaData)
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"delete from [{0}] where [id] = @id and [revision] = @current_revision;", sagaTableName);
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
                    command.CommandText = string.Format(@"delete from [{0}] where [saga_id] = @id", sagaIndexTableName);
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.ExecuteNonQuery();
                }

                commitAction(connection);
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        /// <summary>
        /// Queries the underlying SQL table for the saga whose correlation field has a value
        /// that matches the given field from the incoming message.
        /// </summary>
        public TSagaData Find<TSagaData>(string sagaDataPropertyPath, object fieldFromMessage) where TSagaData : class, ISagaData
        {
            var connection = getConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    if (sagaDataPropertyPath == idPropertyName)
                    {
                        command.CommandText = string.Format(@"select [s].[data] from [{0}] [s] where [s].[id] = @value", sagaTableName);
                    }
                    else
                    {
                        command.CommandText = string.Format(@"select [s].[data] 
                                                    from [{0}] [s] 
                                                        join [{1}] [i] on [s].[id] = [i].[saga_id] 
                                                    where [i].[saga_type] = @saga_type
                                                        and [i].[key] = @key 
                                                        and [i].[value] = @value", sagaTableName, sagaIndexTableName);

                        command.Parameters.AddWithValue("key", sagaDataPropertyPath);
                        command.Parameters.AddWithValue("saga_type", GetSagaTypeName(typeof(TSagaData)));
                    }

                    command.Parameters.AddWithValue("value", (fieldFromMessage ?? "").ToString());

                    var value = (string)command.ExecuteScalar();

                    if (value == null) return null;

                    try
                    {
                        return (TSagaData)JsonConvert.DeserializeObject(value, Settings);
                    }
                    catch (Exception exception)
                    {
                        throw new ApplicationException(
                            string.Format("An error occurred while attempting to deserialize '{0}' into a {1}",
                                value, typeof(TSagaData)), exception);
                    }
                }
            }
            finally
            {
                releaseConnection(connection);
            }
        }

        List<KeyValuePair<string, string>> GetPropertiesToIndex(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            return sagaDataPropertyPathsToIndex
                .Select(path =>
                {
                    var value = Reflect.Value(sagaData, path);

                    return new KeyValuePair<string, string>(path, value != null ? value.ToString() : null);
                })
                .Where(kvp => indexNullProperties || kvp.Value != null)
                .ToList();
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
                var tableNames = connection.GetTableNames();

                // bail out if there's already a table in the database with one of the names
                if (tableNames.Contains(SagaTableName, StringComparer.InvariantCultureIgnoreCase)
                    || tableNames.Contains(SagaIndexTableName, StringComparer.OrdinalIgnoreCase))
                {
                    return this;
                }

                log.Info("Tables '{0}' and '{1}' do not exist - they will be created now",
                         SagaTableName, SagaIndexTableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}] (
	[id] [uniqueidentifier] NOT NULL,
	[revision] [int] NOT NULL,
	[data] [nvarchar](max) NOT NULL,

    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [id] ASC
    ) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

", SagaTableName);
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}] (
	[saga_type] [nvarchar](40) NOT NULL,
	[key] [nvarchar](200) NOT NULL,
	[value] [nvarchar](200) NOT NULL,
	[saga_id] [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [key] ASC,
	    [value] ASC,
	    [saga_type] ASC
    ) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

CREATE NONCLUSTERED INDEX [IX_{0}_saga_id] ON [dbo].[{0}]
(
	[saga_id] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)

", SagaIndexTableName);
                    command.ExecuteNonQuery();
                }

                commitAction(connection);
            }
            finally
            {
                releaseConnection(connection);
            }
            return this;
        }

        /// <summary>
        /// Configures the persister to ignore null-valued correlation properties and not add them to the saga index.
        /// </summary>
        public SqlServerSagaPersister DoNotIndexNullProperties()
        {
            indexNullProperties = false;
            return this;
        }
    }
}