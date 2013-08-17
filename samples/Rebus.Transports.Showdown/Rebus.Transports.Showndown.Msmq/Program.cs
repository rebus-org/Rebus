using Rebus.Configuration;
using Rebus.Transports.Msmq;
using Rebus.Transports.Showdown.Core;
using Rebus.Logging;

namespace Rebus.Transports.Showndown.Msmq
{
    public class Program
    {
        const string SenderInputQueue = "test.showdown.sender";
        const string ReceiverInputQueue = "test.showdown.receiver";

        public static void Main()
        {
            using (var runner = new ShowdownRunner(ReceiverInputQueue))
            {
                PurgeInputQueue(SenderInputQueue);
                PurgeInputQueue(ReceiverInputQueue);

                Configure.With(runner.SenderAdapter)
                         .Logging(l => l.ColoredConsole(LogLevel.Warn))
                         .Transport(t => t.UseMsmq(SenderInputQueue, "error"))
                         .MessageOwnership(o => o.Use(runner))
                         .CreateBus()
                         .Start();

                Configure.With(runner.ReceiverAdapter)
                         .Logging(l => l.ColoredConsole(LogLevel.Warn))
                         .Transport(t => t.UseMsmq(ReceiverInputQueue, "error"))
                         .MessageOwnership(o => o.Use(runner))
                         .CreateBus()
                         .Start();

                runner.Run();
            }
        }

        static void PurgeInputQueue(string inputQueueName)
        {
            using (var queue = new MsmqMessageQueue(inputQueueName))
            {
                queue.PurgeInputQueue();
            }
        }
    }
}
