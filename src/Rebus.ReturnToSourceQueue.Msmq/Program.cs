using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using Rebus.Bus;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using System.Linq;

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

            var dryrun = PromptChar(new[] { 'd', 'm' }, "Perform a (d)ry dun or actually (m) move messages?");

            if (dryrun == 'd')
            {
                parameters.DryRun = true;
            }
            else if (dryrun == 'm')
            {
                parameters.DryRun = false;
            }
            else
            {
                throw new NiceException("Invalid option: {0} - please type (d) to perform a dry run (i.e. don't actually move anything), or (m) to actually move the messages");
            }

            var errorQueueName = Prompt("Please type the name of an error queue");
            parameters.ErrorQueueName = errorQueueName;

            var mode = PromptChar(new[] { 'a', 'p' },
                                  "Move (a)ll messages back to their source queues or (p)rompt for each message");

            if (mode == 'a')
            {
                parameters.AutoMoveAllMessages = true;
            }
            else if (mode == 'p')
            {
                parameters.AutoMoveAllMessages = false;
            }
            else
            {
                throw new NiceException("Invalid option: {0} - please type (a) to move all the messages, or (p) to be prompted for each message", mode);
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

        static char PromptChar(char[] validChars, string question, params object[] objs)
        {
            Console.Write(question, objs);
            Console.Write(" ({0}) > ", string.Join("/", validChars));
            char charToReturn;

            do
            {
                charToReturn = char.ToLowerInvariant(Console.ReadKey(true).KeyChar);
            } while (!validChars.Select(char.ToLowerInvariant).Contains(charToReturn));

            Console.WriteLine(charToReturn);

            return charToReturn;
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
                        if (!transportMessageToSend.Headers.ContainsKey(Headers.SourceQueue))
                        {
                            throw new NiceException(
                                "Message {0} does not have a source queue header - it will be moved back to the input queue",
                                message.Id);
                        }

                        var sourceQueue = (string)transportMessageToSend.Headers[Headers.SourceQueue];

                        if (parameters.AutoMoveAllMessages)
                        {
                            msmqMessageQueue.Send(sourceQueue, transportMessageToSend, transactionContext);

                            Print("Moved {0} to {1}", message.Id, sourceQueue);
                        }
                        else
                        {
                            var answer = PromptChar(new[] { 'y', 'n' }, "Would you like to move {0} to {1}? (y/n)",
                                                    message.Id, sourceQueue);

                            if (answer == 'y')
                            {
                                msmqMessageQueue.Send(sourceQueue, transportMessageToSend, transactionContext);

                                Print("Moved {0} to {1}", message.Id, sourceQueue);
                            }
                            else
                            {
                                msmqMessageQueue.Send(msmqMessageQueue.InputQueueAddress,
                                                      transportMessageToSend,
                                                      transactionContext);

                                Print("Moved {0} to {1}", message.Id, msmqMessageQueue.InputQueueAddress);
                            }
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
                    var answer = PromptChar(new[] {'y', 'n'}, "Would you like to commit the queue transaction?");

                    if (answer == 'y')
                    {
                        Print("Committing queue transaction");

                        tx.Complete();
                    }
                    else
                    {
                        Print("Queue transaction aborted");
                    }
                }
                else
                {
                    Print("Aborting queue transaction");
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
