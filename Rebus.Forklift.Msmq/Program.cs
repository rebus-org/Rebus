using System;
using System.Collections.Generic;
using System.Linq;
using GoCommando;
using GoCommando.Api;
using GoCommando.Attributes;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;
using Rebus.Transport.Msmq;

namespace Rebus.Forklift.Msmq
{
    [Banner(@"Rebus Forklift - simple message mover - MSMQ edition")]
    class Program : ICommando
    {
        [PositionalArgument]
        [Description("Name of queue to receive messages from")]
        [Example("some_queue")]
        [Example("remote_queue@another_machine")]
        public string InputQueue { get; set; }

        [NamedArgument("output", "o")]
        [Description("Default queue to forward messages to")]
        [Example("another_queue")]
        [Example("remote_queue@another_machine")]
        public string DefaultOutputQueue { get; set; }

        static void Main(string[] args)
        {
            Go.Run<Program>(args);
        }

        readonly HashSet<string> _initializedQueues = new HashSet<string>();

        public void Run()
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();

            PrintLine("Will start receiving messages from '{0}'", InputQueue);

            if (DefaultOutputQueue != null)
            {
                PrintLine("(will provide '{0}' as the default queue to forward messages to)", DefaultOutputQueue);
            }

            PrintLine();

            var transport = new MsmqTransport(InputQueue);

            while (true)
            {
                using (var transactionContext = new DefaultTransactionContext())
                {
                    var transportMessage = transport.Receive(transactionContext).Result;

                    if (transportMessage == null) break;

                    try
                    {
                        HandleMessage(transportMessage, transport, transactionContext);

                        transactionContext.Complete().Wait();
                    }
                    catch (Exception exception)
                    {
                        PrintLine("Failed: {0}", exception.Message);
                    }
                }
            }
        }

        void HandleMessage(TransportMessage transportMessage, MsmqTransport transport, ITransactionContext transactionContext)
        {
            var message = transportMessage.GetMessageLabel();

            var options = new List<KeyOption>();

            if (DefaultOutputQueue != null)
            {
                options.Add(KeyOption.New('d',
                    () => { MoveMessage(transport, transportMessage, transactionContext, DefaultOutputQueue); },
                    "Move to default queue '{0}'", DefaultOutputQueue));
            }

            if (transportMessage.Headers.ContainsKey(Headers.SourceQueue))
            {
                var sourceQueue = transportMessage.Headers[Headers.SourceQueue];

                options.Add(KeyOption.New('s',
                    () => { MoveMessage(transport, transportMessage, transactionContext, sourceQueue); },
                    "Return to source queue '{0}'", sourceQueue));
            }

            options.Add(KeyOption.New('c', () =>
            {
                Console.Write("queue > ");
                var queueName = Console.ReadLine();
                MoveMessage(transport, transportMessage, transactionContext, queueName);
            }, "Enter custom queue name to move message to"));

            Prompt(message, options);

            PrintLine();
        }

        void MoveMessage(MsmqTransport transport, TransportMessage transportMessage, ITransactionContext transactionContext, string destinationQueue)
        {
            if (!_initializedQueues.Contains(destinationQueue))
            {
                transport.CreateQueue(destinationQueue);
                _initializedQueues.Add(destinationQueue);
            }

            Print("  => '{0}' - ", destinationQueue);

            try
            {
                transport.Send(destinationQueue, transportMessage, transactionContext).Wait();

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

        class KeyOption
        {
            public char KeyChar { get; private set; }
            public Action Action { get; private set; }
            public string Description { get; private set; }

            KeyOption(char keyChar, Action action, string description)
            {
                if (action == null) throw new ArgumentNullException("action");
                KeyChar = keyChar;
                Action = action;
                Description = description;
            }

            public static KeyOption New(char keyChar, Action action, string description, params object[] objs)
            {
                return new KeyOption(keyChar, action, string.Format(description, objs));
            }

            public override string ToString()
            {
                return string.Format("({0}) {1}", char.ToUpperInvariant(KeyChar), Description);
            }
        }
    }
}
