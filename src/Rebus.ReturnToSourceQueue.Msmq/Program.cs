using System;
using System.Collections.Generic;
using System.Transactions;
using Rebus.Bus;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.ReturnToSourceQueue.Msmq
{
    class Program
    {
        static int Main(string[] args)
        {
            var parameters = args.Length == 0
                                 ? PromptArgs()
                                 : ParseArgs(args);
            try
            {
                Run(parameters);

                return 0;
            }
            catch (NiceException e)
            {
                Print(e.Message);

                return -1;
            }
            catch (Exception e)
            {
                Print(e.ToString());

                return -2;
            }
        }

        static Parameters PromptArgs()
        {
            var parameters = new Parameters { Interactive = false };

            var dryrun = Prompt("Perform a (d)ry dun or actually (m) move messages?");

            if (dryrun.ToLowerInvariant() == "d")
            {
                parameters.DryRun = true;
            }
            else if (dryrun.ToLowerInvariant() == "m")
            {
                parameters.DryRun = false;
            }
            else
            {
                throw new NiceException("Invalid option: {0} - please type D to perform a dry run (i.e. don't actually move anything");
            }

            var errorQueueName = Prompt("Please type the name of an error queue");
            parameters.ErrorQueueName = errorQueueName;

            var mode = Prompt("Move (a)ll messages back to their source queues or (p)rompt for each message");

            if (mode.ToLowerInvariant() == "a")
            {
                parameters.AutoMoveAllMessages = true;
            }
            else if (mode.ToLowerInvariant() == "p")
            {
                parameters.AutoMoveAllMessages = false;
            }
            else
            {
                throw new NiceException("Invalid option: {0} - please type A to move all the messages, or P to be prompted for each message", mode);
            }

            return parameters;
        }

        static Parameters ParseArgs(string[] args)
        {
            return new Parameters
                       {
                           Interactive = false
                       };
        }

        static string Prompt(string question, params object[] objs)
        {
            Console.Write(question, objs);
            Console.Write(" > ");
            return Console.ReadLine();
        }

        static void Print(string message, params object[] objs)
        {
            Console.WriteLine(message, objs);
        }

        class Parameters
        {
            public bool Interactive { get; set; }

            public string ErrorQueueName { get; set; }

            public bool AutoMoveAllMessages { get; set; }

            public bool DryRun { get; set; }
        }

        class NiceException : ApplicationException
        {
            public NiceException(string message, params object[] objs)
                : base(string.Format(message, objs))
            {
            }
        }

        static void Run(Parameters parameters)
        {
            Console.WriteLine(@"Running with
interactive: {0}
eror queue: {1}",
                              parameters.Interactive,
                              parameters.ErrorQueueName);

            if (string.IsNullOrWhiteSpace(parameters.ErrorQueueName))
            {
                throw new NiceException("Please specify the name of an error queue");
            }

            using (var tx = new TransactionScope())
            {
                var transactionContext = new AmbientTransactionContext();
                var msmqMessageQueue = new MsmqMessageQueue(parameters.ErrorQueueName);
                var allTheMessages = GetAllTheMessages(msmqMessageQueue, transactionContext);

                foreach (var message in allTheMessages)
                {
                    var transportMessageToSend = message.ToForwardableMessage();

                    try
                    {
                        if (parameters.AutoMoveAllMessages)
                        {
                            if (transportMessageToSend.Headers.ContainsKey(Headers.SourceQueue))
                            {
                                var sourceQueue = (string) transportMessageToSend.Headers[Headers.SourceQueue];

                                msmqMessageQueue.Send(sourceQueue, transportMessageToSend, transactionContext);

                                Print("Moved {0} to {1}", message.Id, sourceQueue);
                            }
                            else
                            {
                                throw new NiceException(
                                    "Message {0} does not have a source queue header - it will be moved back to the input queue",
                                    message.Id);
                            }
                        }
                        else
                        {
                            throw new ApplicationException("Prompt mode does not work right now - sorry");
                        }
                    }
                    catch (NiceException e)
                    {
                        Print(e.Message);

                        msmqMessageQueue.Send(msmqMessageQueue.InputQueueAddress,
                                              transportMessageToSend,
                                              transactionContext);
                    }
                }

                if (!parameters.DryRun)
                {
                    tx.Complete();
                }
            }
        }

        static List<ReceivedTransportMessage> GetAllTheMessages(MsmqMessageQueue msmqMessageQueue, ITransactionContext transactionContext)
        {
            var messages = new List<ReceivedTransportMessage>();
            ReceivedTransportMessage transportMessage;

            while ((transportMessage = msmqMessageQueue.ReceiveMessage(transactionContext)) != null)
            {
                messages.Add(transportMessage);
            }

            return messages;
        }
    }
}
