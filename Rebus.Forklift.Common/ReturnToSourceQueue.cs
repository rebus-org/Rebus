using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Forklift.Common
{
    public class ReturnToSourceQueue
    {
        readonly HashSet<string> _initializedQueues = new HashSet<string>();
        readonly ITransport _transport;

        public ReturnToSourceQueue(ITransport transport)
        {
            _transport = transport;
        }

        public string DefaultOutputQueue { get; set; }

        public string InputQueue { get; set; }

        public void HandleMessage(TransportMessage transportMessage, ITransactionContext transactionContext)
        {
            var message = transportMessage.GetMessageLabel();

            var options = new List<KeyOption>();

            if (DefaultOutputQueue != null)
            {
                options.Add(KeyOption.New('d',
                    () => { MoveMessage(transportMessage, transactionContext, DefaultOutputQueue); },
                    "Move to default queue '{0}'", DefaultOutputQueue));
            }

            if (transportMessage.Headers.ContainsKey(Headers.SourceQueue))
            {
                var sourceQueue = transportMessage.Headers[Headers.SourceQueue];

                options.Add(KeyOption.New('s',
                    () => { MoveMessage(transportMessage, transactionContext, sourceQueue); },
                    "Return to source queue '{0}'", sourceQueue));
            }

            options.Add(KeyOption.New('c', () =>
            {
                Console.Write("queue > ");
                var queueName = Console.ReadLine();
                MoveMessage(transportMessage, transactionContext, queueName);
            }, "Enter custom queue name to move message to"));

            Prompt(message, options);

            PrintLine();
        }

        void MoveMessage(TransportMessage transportMessage, ITransactionContext transactionContext, string destinationQueue)
        {
            if (!_initializedQueues.Contains(destinationQueue))
            {
                _transport.CreateQueue(destinationQueue);
                _initializedQueues.Add(destinationQueue);
            }

            Print("  => '{0}' - ", destinationQueue);

            try
            {
                _transport.Send(destinationQueue, transportMessage, transactionContext).Wait();

                PrintLine("OK");
            }
            catch (Exception exception)
            {
                PrintLine(exception.Message);
                throw;
            }
        }

        void PrintLine()
        {
            Console.WriteLine();
        }

        void PrintLine(string message, params object[] objs)
        {
            Console.WriteLine(message, objs);
        }

        void Print(string message, params object[] objs)
        {
            Console.Write(message, objs);
        }

        void Prompt(string message, IEnumerable<KeyOption> availableOptions)
        {
            var options = availableOptions.ToList();

            PrintLine(@"{0}
{1}", message, string.Join(Environment.NewLine, options.Select(o => "  " + o.ToString())));

            while (true)
            {
                var key = char.ToLowerInvariant(Console.ReadKey(true).KeyChar);
                var selectedOption = options.FirstOrDefault(o => char.ToLowerInvariant(o.KeyChar) == key);

                if (selectedOption == null) continue;

                selectedOption.Action();
                break;
            }
        }

        public void Run()
        {
            PrintLine("Will start receiving messages from '{0}'", InputQueue);

            if (DefaultOutputQueue != null)
            {
                PrintLine("(will provide '{0}' as the default queue to forward messages to)", DefaultOutputQueue);
            }

            PrintLine();

            while (true)
            {
                using (var transactionContext = new DefaultTransactionContext())
                {
                    var transportMessage = _transport.Receive(transactionContext).Result;

                    if (transportMessage == null) break;

                    try
                    {
                        HandleMessage(transportMessage, transactionContext);

                        transactionContext.Complete().Wait();
                    }
                    catch (Exception exception)
                    {
                        PrintLine("Failed: {0}", exception.Message);
                    }
                }
            }
        }
    }
}
