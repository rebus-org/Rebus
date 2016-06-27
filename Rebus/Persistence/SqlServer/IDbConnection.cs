using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Wrapper of <see cref="SqlConnection"/> that allows for easily changing how transactions are handled, and possibly how <see cref="SqlConnection"/> instances
    /// are reused by various services
    /// </summary>
    public interface IDbConnection : IDisposable
    {
        /// <summary>
        /// Creates a ready to used <see cref="SqlCommand"/>
        /// </summary>
        SqlCommand CreateCommand();

        /// <summary>
        /// Gets the names of all the tables in the current database for the current schema
        /// </summary>
        IEnumerable<string> GetTableNames();
        
        /// <summary>
        /// Marks that all work has been successfully done and the <see cref="SqlConnection"/> may have its transaction committed or whatever is natural to do at this time
        /// </summary>
        Task Complete();

        /// <summary>
        /// Gets information about the columns in the table given by <paramref name="dataTableName"/>
        /// </summary>
        IEnumerable<DbColumn> GetColumns(string dataTableName);
    }

    /// <summary>
    /// Represents a SQL Server column
    /// </summary>
    public class DbColumn
    {
        /// <summary>
        /// Gets the name of the column
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets the SQL datatype of the column
        /// </summary>
        public SqlDbType Type { get; }

        /// <summary>
        /// Creates the column
        /// </summary>
        public DbColumn(string name, SqlDbType type)
        {
            Name = name;
            Type = type;
        }
    }
}