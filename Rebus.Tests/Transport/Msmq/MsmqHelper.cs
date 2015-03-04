using System;

namespace Rebus.Tests.Transport.Msmq
{
    public class MsmqHelper
    {
        public static string QueueName(string nameBase)
        {
            var queueName = GenerateQueueName(nameBase);
            Console.WriteLine("Using MSMQ queue {0}", queueName);
            return queueName;
        }

        static string GenerateQueueName(string nameBase)
        {
            if (nameBase.Contains("@"))
            {
                var tokens = nameBase.Split('@');

                return string.Format("{0}{1}@{2}", tokens[0], TestConfig.Suffix, tokens[1]);
            }
            return string.Format("{0}{1}", nameBase, TestConfig.Suffix);
        }
    }
}