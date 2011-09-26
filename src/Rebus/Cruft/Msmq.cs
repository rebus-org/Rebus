using System;
using System.Messaging;

namespace Rebus.Cruft
{
    public class Msmq : IDisposable
    {
        readonly MessageQueue messageQueue;

        public Msmq(string path)
        {
            messageQueue = MessageQueue.Exists(path)
                               ? new MessageQueue(path)
                               : MessageQueue.Create(path, transactional: true);

            messageQueue.Formatter = new XmlMessageFormatter(new[] { typeof(string) });
        }

        public void Send(object obj)
        {
            using (var messageQueueTransaction = new MessageQueueTransaction())
            {
                messageQueueTransaction.Begin();
                messageQueue.Send(obj, messageQueueTransaction);
                messageQueueTransaction.Commit();
            }
        }

        public object Receive()
        {
            try
            {
                var asyncResult = messageQueue.BeginReceive(TimeSpan.FromSeconds(1));
                var message = messageQueue.EndReceive(asyncResult);
                return message.Body;
            }
            catch (MessageQueueException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public void Dispose()
        {
            messageQueue.Dispose();
        }

        public class Sender : IDisposable, ISendMessages
        {
            readonly MessageQueue messageQueue;

            public Sender(MessageQueue messageQueue)
            {
                this.messageQueue = messageQueue;
            }

            public void Send(object obj, MessageQueueTransaction transaction)
            {
                messageQueue.Send(obj, transaction);
            }

            public void Dispose()
            {
                messageQueue.Dispose();
            }

            public void Send(object message)
            {
                throw new NotImplementedException();
            }
        }

        public class Receiver : IDisposable
        {
            readonly MessageQueue messageQueue;
            MessageQueueTransaction messageQueueTransaction;

            public Receiver(MessageQueue messageQueue)
            {
                this.messageQueue = messageQueue;
            }

            public void Receive(Action<object> messageDispatcher)
            {
                using (messageQueueTransaction = new MessageQueueTransaction())
                {
                    messageQueueTransaction.Begin();
                    try
                    {
                        var message = messageQueue.Receive(TimeSpan.FromSeconds(2), messageQueueTransaction);
                        if (message != null)
                        {
                            var body = message.Body;
                            messageDispatcher(body);
                        }
                        messageQueueTransaction.Commit();
                    }
                    catch (MessageQueueException e)
                    {
                        if (!e.Message.ToLower().Contains("timeout"))
                        {
                            Console.WriteLine(e);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        messageQueueTransaction.Abort();
                    }
                }
            }

            public void Dispose()
            {
                messageQueue.Dispose();
            }
        }
    }
}