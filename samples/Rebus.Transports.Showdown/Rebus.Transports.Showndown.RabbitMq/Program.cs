using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Transports.Showdown.Core;
using Rebus.RabbitMQ;

namespace Rebus.Transports.Showndown.RabbitMq
{
    public class Program
    {
        const string SenderInputQueue = "test.showdown.sender";
        const string ReceiverInputQueue = "test.showdown.receiver";
        const string RabbitMqConnectionString = "amqp://localhost";

        public static void Main()
        {
            using (var runner = new ShowdownRunner(ReceiverInputQueue))
            {
                Configure.With(runner.SenderAdapter)
                    .Logging(l => l.ColoredConsole(LogLevel.Warn))
                    .Transport(t => t.UseRabbitMq(RabbitMqConnectionString, SenderInputQueue, "error")
                        .PurgeInputQueue())
                    .MessageOwnership(o => o.Use(runner))
                    .CreateBus()
                    .Start();

                Configure.With(runner.ReceiverAdapter)
                    .Logging(l => l.ColoredConsole(LogLevel.Warn))
                    .Transport(t => t.UseRabbitMq(RabbitMqConnectionString, ReceiverInputQueue, "error")
                        .PurgeInputQueue())
                    .MessageOwnership(o => o.Use(runner))
                    .CreateBus()
                    .Start();

                runner.Run();
            }
        }
    }
}
