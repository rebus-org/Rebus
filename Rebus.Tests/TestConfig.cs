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
    }
}