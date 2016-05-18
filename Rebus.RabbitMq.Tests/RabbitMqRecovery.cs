using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Retry.Simple;
using Rebus.Tests;
using Rebus.Tests.Extensions;

namespace Rebus.RabbitMq.Tests
{
    [TestFixture]
    public class RabbitMqRecovery : FixtureBase
    {
        static readonly string ConnectionString = RabbitMqTransportFactory.ConnectionString;
        static readonly string QueueName = TestConfig.QueueName("recoverytest");

        [Test, Ignore("Only meant to be run manually, with Administrator priviledges")]
        public void VerifyThatEndpointCanRecoverAfterLosingRabbitMqConnection()
        {
            const int numberOfMessages = 100;
            const int millisecondsDelay = 300;

            var expectedTestDuration = TimeSpan.FromMilliseconds(numberOfMessages * millisecondsDelay);

            Console.WriteLine($"Expected test duration {expectedTestDuration}");

            using (var activator = new BuiltinHandlerActivator())
            {
                var receivedMessages = 0;
                var allMessagesReceived = new ManualResetEvent(false);

                activator.Handle<string>(async message =>
                {
                    await Task.Delay(millisecondsDelay);

                    receivedMessages++;

                    if (receivedMessages == numberOfMessages)
                    {
                        allMessagesReceived.Set();
                    }
                });

                Configure.With(activator)
                    .Logging(l => l.Console(LogLevel.Warn))
                    .Transport(t => t.UseRabbitMq(ConnectionString, QueueName))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(0);
                        o.SetMaxParallelism(1);
                        o.SimpleRetryStrategy(maxDeliveryAttempts: 1);
                    })
                    .Start();

                Console.WriteLine($"Sending {numberOfMessages} messages");

                Enumerable.Range(0, numberOfMessages)
                    .Select(i => $"this is message {i}")
                    .ToList()
                    .ForEach(message => activator.Bus.SendLocal(message).Wait());

                Console.WriteLine("Starting receiver");

                activator.Bus.Advanced.Workers.SetNumberOfWorkers(1);

                Console.WriteLine("Waiting a short while");

                Thread.Sleep(5000);

                Console.WriteLine("Stopping RabbitMQ service");

                Execute("net stop rabbitmq");

                Console.WriteLine("Waiting a short while");

                Thread.Sleep(5000);

                Console.WriteLine("Starting RabbitMQ service");

                Execute("net start rabbitmq");

                Console.WriteLine("Waiting for the last messages");

                allMessagesReceived.WaitOrDie(TimeSpan.FromMinutes(5));
            }
        }

        static void Execute(string shellCommand)
        {
            try
            {
                Console.WriteLine($"C:\\> {shellCommand}");

                var parts = shellCommand.Split(' ');

                Process.Start(new ProcessStartInfo
                {
                    FileName = parts.First(),
                    Arguments = string.Join(" ", parts.Skip(1))
                });
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Could not execute shell command '{shellCommand}'", exception);
            }
        }
    }
}