using System;

namespace Rebus.Shared
{
    static internal class MsmqUtil
    {
        public static string GetPath(string inputQueue)
        {
            if (inputQueue.Contains("@"))
            {
                inputQueue = ParseQueueName(inputQueue);
            }
            else
            {
                inputQueue = AssumeLocalQueue(inputQueue);
            }
            return inputQueue;
        }

        static string ParseQueueName(string inputQueue)
        {
            var tokens = inputQueue.Split('@');

            if (tokens.Length != 2)
            {
                throw new ArgumentException(String.Format("The specified MSMQ input queue is invalid!: {0}", inputQueue));
            }

            return String.Format(@"{0}\private$\{1}", tokens[0], tokens[1]);
        }

        static string AssumeLocalQueue(string inputQueue)
        {
            return String.Format(@".\private$\{0}", inputQueue);
        }
    }
}