using System;
using System.Messaging;

namespace Rebus.Shared
{
    static internal class MsmqUtil
    {
        public static void PurgeQueue(string queueName)
        {
            var path = GetPath(queueName);
            
            if (!MessageQueue.Exists(path)) return;

            using(var messageQueue = new MessageQueue(path))
            {
                messageQueue.Purge();
            }
        }

        public static string GetPath(string queueName)
        {
            if (queueName.Contains("@"))
            {
                queueName = ParseQueueName(queueName);
            }
            else
            {
                queueName = AssumeLocalQueue(queueName);
            }
            return queueName;
        }

        static string ParseQueueName(string inputQueue)
        {
            var tokens = inputQueue.Split('@');

            if (tokens.Length != 2)
            {
                throw new ArgumentException(String.Format("The specified MSMQ input queue is invalid!: {0}", inputQueue));
            }

            return string.Format(@"{0}\private$\{1}", tokens[0], tokens[1]);
        }

        static string AssumeLocalQueue(string inputQueue)
        {
            return string.Format(@".\private$\{0}", inputQueue);
        }
    }
}