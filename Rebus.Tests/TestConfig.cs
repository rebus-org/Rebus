using System;

namespace Rebus.Tests
{
    public class TestConfig
    {
        /// <summary>
        /// Gets a suffix that can be appended to things in order to have tests run on separate sets of queues/databases/whatever
        /// </summary>
        public static string Suffix
        {
            get
            {
                var agentSpecificVariable = Environment.GetEnvironmentVariable("tcagent");

                if (agentSpecificVariable != null)
                {
                    return agentSpecificVariable;
                }

                return "";
            }
        }

        /// <summary>
        /// Gets a (possibly agent-qualified) queue name, which allows for tests to run in parallel
        /// </summary>
        public static string QueueName(string nameBase)
        {
            var queueName = GenerateQueueName(nameBase);
            
            Console.WriteLine("Using queue name {0}", queueName);
            
            return queueName;
        }

        static string GenerateQueueName(string nameBase)
        {
            if (nameBase.Contains("@"))
            {
                var tokens = nameBase.Split('@');

                return string.Format("{0}{1}@{2}", tokens[0], Suffix, tokens[1]);
            }
            return string.Format("{0}{1}", nameBase, Suffix);
        }
    }
}