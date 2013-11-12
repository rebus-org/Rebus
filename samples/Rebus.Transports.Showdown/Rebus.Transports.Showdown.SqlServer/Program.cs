using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Transports.Showdown.Core;
using Rebus.Transports.Sql;

namespace Rebus.Transports.Showdown.SqlServer
{
    public class Program
    {
        const string SenderInputQueue = "test.showdown.sender";
        const string ReceiverInputQueue = "test.showdown.receiver";
        const string SqlServerConnectionString = "server=.;initial catalog=rebus_test;integrated security=sspi";

        const string MessageTableName = SqlServerMessageQueueConfigurationExtension
            .DefaultMessagesTableName;

        public static void Main()
        {
            using (var runner = new ShowdownRunner(ReceiverInputQueue))
            {
                PurgeInputQueue(SenderInputQueue);
                PurgeInputQueue(ReceiverInputQueue);

                Configure.With(runner.SenderAdapter)
                         .Logging(l => l.ColoredConsole(LogLevel.Warn))
                         .Transport(t => t.UseSqlServer(SqlServerConnectionString, SenderInputQueue, "error"))
                         .MessageOwnership(o => o.Use(runner))
                         .CreateBus()
                         .Start();

                Configure.With(runner.ReceiverAdapter)
                         .Logging(l => l.ColoredConsole(LogLevel.Warn))
                         .Transport(t => t.UseSqlServer(SqlServerConnectionString, ReceiverInputQueue, "error"))
                         .MessageOwnership(o => o.Use(runner))
                         .CreateBus()
                         .Start();

                runner.Run();
            }
        }

        static void PurgeInputQueue(string inputQueueName)
        {
            var queue = new SqlServerMessageQueue(SqlServerConnectionString,
                MessageTableName,
                inputQueueName);
            
            queue.PurgeInputQueue();
        }
    }
}
