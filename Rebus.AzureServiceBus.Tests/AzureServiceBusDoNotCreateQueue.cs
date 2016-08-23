using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Config;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests
{

    [TestFixture, Category(TestCategory.Azure)]
    public class BasicAzureServiceBusBasicReceiveOnly : FixtureBase
    {
        static readonly string QueueName = TestConfig.QueueName("input");
        CancellationToken _cancellationToken;

        protected override void SetUp()
        {
            _cancellationToken = new CancellationTokenSource().Token;
        }

        [Test]
        [TestCase(AzureServiceBusMode.Basic, 5)]
        [TestCase(AzureServiceBusMode.Basic, 10)]
        [TestCase(AzureServiceBusMode.Standard, 5)]
        [TestCase(AzureServiceBusMode.Standard, 10)]
        public async void DoesntIgnoreDefinedTimeoutWhenReceiving(AzureServiceBusMode mode, int operationTimeoutInSeconds)
        {
            var operationTimeout = TimeSpan.FromSeconds(operationTimeoutInSeconds);

            var connString = StandardAzureServiceBusTransportFactory.ConnectionString;
            var builder = new ServiceBusConnectionStringBuilder(connString)
            {
                OperationTimeout = operationTimeout
            };
            var newConnString = builder.ToString();

            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var transport = new AzureServiceBusTransport(newConnString, QueueName, consoleLoggerFactory, new TplAsyncTaskFactory(consoleLoggerFactory));

            Using(transport);

            transport.PurgeInputQueue();
            //Create the queue for the receiver since it cannot create it self beacuse of lacking rights on the namespace
            transport.CreateQueue(QueueName);

            var senderActivator = new BuiltinHandlerActivator();

            var senderBus = Configure.With(senderActivator)
                .Transport(t => t.UseAzureServiceBus(newConnString, "sender", mode))
                .Start();

            Using(senderBus);

            // queue 3 messages
            await senderBus.Advanced.Routing.Send(QueueName, "message to receiver");
            await senderBus.Advanced.Routing.Send(QueueName, "message to receiver2");
            await senderBus.Advanced.Routing.Send(QueueName, "message to receiver3");

            await Task.Delay(TimeSpan.FromSeconds(2)); // wait a bit to make sure the messages are queued.

            // receive 1
            using (var transactionContext = new DefaultTransactionContext())
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var msg = await transport.Receive(transactionContext, _cancellationToken);
                sw.Stop();
                await transactionContext.Complete();

                msg.Should().NotBeNull();
                sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(1500));
            }

            // receive 2
            using (var transactionContext = new DefaultTransactionContext())
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var msg = await transport.Receive(transactionContext, _cancellationToken);
                sw.Stop();
                await transactionContext.Complete();

                msg.Should().NotBeNull();
                sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(1500));
            }

            // receive 3
            using (var transactionContext = new DefaultTransactionContext())
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var msg = await transport.Receive(transactionContext, _cancellationToken);
                sw.Stop();
                await transactionContext.Complete();

                msg.Should().NotBeNull();
                sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(1500));
            }

            // receive 4 - NOTHING
            using (var transactionContext = new DefaultTransactionContext())
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var msg = await transport.Receive(transactionContext, _cancellationToken);
                sw.Stop();
                await transactionContext.Complete();

                msg.Should().BeNull();
                sw.Elapsed.Should().BeCloseTo(operationTimeout, 2000);
            }

            // put 1 more message 
            await senderBus.Advanced.Routing.Send(QueueName, "message to receiver5");

            await Task.Delay(TimeSpan.FromSeconds(2)); // wait a bit to make sure the messages are queued.

            // receive 5
            using (var transactionContext = new DefaultTransactionContext())
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var msg = await transport.Receive(transactionContext, _cancellationToken);
                sw.Stop();
                await transactionContext.Complete();

                msg.Should().NotBeNull();
                sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(1500));
            }

            // receive 6 - NOTHING
            using (var transactionContext = new DefaultTransactionContext())
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var msg = await transport.Receive(transactionContext, _cancellationToken);
                sw.Stop();
                await transactionContext.Complete();

                msg.Should().BeNull();
                sw.Elapsed.Should().BeCloseTo(operationTimeout, 2000);
            }
        }

        [Test]
        [TestCase(AzureServiceBusMode.Basic)]
        [TestCase(AzureServiceBusMode.Standard)]
        public async void ShouldBeAbleToRecieveEvenWhenNotCreatingQueue(AzureServiceBusMode mode)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var transport = new AzureServiceBusTransport(StandardAzureServiceBusTransportFactory.ConnectionString, QueueName, consoleLoggerFactory, new TplAsyncTaskFactory(consoleLoggerFactory));
            transport.PurgeInputQueue();
            //Create the queue for the receiver since it cannot create it self beacuse of lacking rights on the namespace
            transport.CreateQueue(QueueName);

            var recieverActivator = new BuiltinHandlerActivator();
            var senderActivator = new BuiltinHandlerActivator();

            var receiverBus = Configure.With(recieverActivator)
                .Logging(l => l.ColoredConsole())
                .Transport(t =>
                    t.UseAzureServiceBus(StandardAzureServiceBusTransportFactory.ConnectionString, QueueName)
                        .DoNotCreateQueues())
                .Start();

            var senderBus = Configure.With(senderActivator)
                .Transport(t => t.UseAzureServiceBus(StandardAzureServiceBusTransportFactory.ConnectionString, "sender", mode))
                .Start();

            Using(receiverBus);
            Using(senderBus);

            var gotMessage = new ManualResetEvent(false);

            recieverActivator.Handle<string>(async (bus, context, message) =>
            {
                gotMessage.Set();
                Console.WriteLine("got message in readonly mode");
            });
            await senderBus.Advanced.Routing.Send(QueueName, "message to receiver");

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(10));


        }
    }

    public class NotCreatingQueueTest : FixtureBase
    {
        [TestCase(AzureServiceBusMode.Basic)]
        [TestCase(AzureServiceBusMode.Standard)]
        public void ShouldNotCreateInputQueueWhenConfiguredNotTo(AzureServiceBusMode mode)
        {
            var manager = NamespaceManager.CreateFromConnectionString(StandardAzureServiceBusTransportFactory.ConnectionString);
            var queueName = Guid.NewGuid().ToString("N");

            Assert.IsFalse(manager.QueueExists(queueName));

            var recieverActivator = new BuiltinHandlerActivator();
            var bus = Configure.With(recieverActivator)
                .Logging(l => l.ColoredConsole())
                .Transport(t =>
                    t.UseAzureServiceBus(StandardAzureServiceBusTransportFactory.ConnectionString, queueName, mode)
                        .DoNotCreateQueues())
                .Start();
           
                Assert.IsFalse(manager.QueueExists(queueName));
           
            Using(recieverActivator);
            Using(bus);

        }
    }
}
