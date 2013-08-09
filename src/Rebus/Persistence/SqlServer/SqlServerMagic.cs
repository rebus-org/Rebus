using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerMagic
    {
        static readonly PropertyInfo ConnectionInfo = typeof(SqlConnection).GetProperty("InnerConnection", BindingFlags.NonPublic | BindingFlags.Instance);

        protected const int PrimaryKeyViolationNumber = 2627;

        protected void AssignTransactionIfNecessary(SqlConnection connection, SqlCommand command)
        {
            var transactionOrNull = GetTransactionOrNull(connection);
            if (transactionOrNull == null) return;

            // LOOOOOOOOOOOOOOOOLOOOOOOOOOOOOOOOLOL SILLY ADO.NET WHY U NO DO THIS YOUR FUCKING SELF WHEN GIVING ME THE COMMAND???!?!?!?
            command.Transaction = transactionOrNull;
        }

        /// <summary>
        /// BAM!1 get current transaction by applying a few select reflection spells
        /// </summary>
        SqlTransaction GetTransactionOrNull(IDbConnection conn)
        {
            try
            {
                var internalConn = ConnectionInfo.GetValue(conn, null);
                var currentTransactionProperty = internalConn.GetType().GetProperty("CurrentTransaction", BindingFlags.NonPublic | BindingFlags.Instance);
                if (currentTransactionProperty == null)
                {
                    throw new ApplicationException(string.Format("Failed to retrieve the CurrentTransaction property of the current connection of type {0}", internalConn.GetType()));
                }

                var currentTransaction = currentTransactionProperty.GetValue(internalConn, null);
                if (currentTransaction == null)
                {
                    return null;
                }

                var realTransactionProperty = currentTransaction.GetType().GetProperty("Parent", BindingFlags.NonPublic | BindingFlags.Instance);
                if (realTransactionProperty == null)
                {
                    throw new ApplicationException(string.Format("Failed to retrieve the Parent property of the current internal transaction of type {0}", currentTransaction.GetType()));
                }

                var realTransaction = realTransactionProperty.GetValue(currentTransaction, null);

                return (SqlTransaction)realTransaction;
            }
            catch (Exception e)
            {
                throw new ApplicationException(string.Format("An error occurred while attempting to retrieve the current transaction from {0}." +
                                                             " Be warned that this way of retrieving the transaction might be brittle with respect" +
                                                             " to the evolution of ADO.NET - who knows, one day the current transaction might not" +
                                                             " be accessed this easily..... </sarcasm>", conn), e);
            }
        }
    }
}