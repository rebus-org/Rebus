namespace Rebus.Transports.Sql
{
    /// <summary>
    /// Options thingie that allows for calling methods of <see cref="SqlServerMessageQueue"/> from the configuration API
    /// </summary>
    public class SqlServerMessageQueueOptions
    {
        readonly SqlServerMessageQueue sqlServerMessageQueue;

        internal SqlServerMessageQueueOptions(SqlServerMessageQueue sqlServerMessageQueue)
        {
            this.sqlServerMessageQueue = sqlServerMessageQueue;
        }

        /// <summary>
        /// Deletes all the messages from the message table that have the current input queue specified as the recipient
        /// </summary>
        public SqlServerMessageQueueOptions PurgeInputQueue()
        {
            sqlServerMessageQueue.PurgeInputQueue();
            return this;
        }

        /// <summary>
        /// Creates the message table if a table with that name does not already exist
        /// </summary>
        public SqlServerMessageQueueOptions EnsureTableIsCreated()
        {
            sqlServerMessageQueue.EnsureTableIsCreated();
            return this;
        }
    }
}