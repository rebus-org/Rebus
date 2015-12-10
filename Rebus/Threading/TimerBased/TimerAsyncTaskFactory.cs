using System;
using System.Threading.Tasks;
using Rebus.Logging;

namespace Rebus.Threading.TimerBased
{
    public class TimerAsyncTaskFactory : IAsyncTaskFactory
    {
        readonly IRebusLoggerFactory _rebusLoggerFactory;

        public TimerAsyncTaskFactory(IRebusLoggerFactory rebusLoggerFactory)
        {
            _rebusLoggerFactory = rebusLoggerFactory;
        }

        public IAsyncTask Create(string description, Func<Task> action, bool prettyInsignificant = false, int intervalSeconds = 10)
        {
            return new TimerAsyncTask(description, action, _rebusLoggerFactory, prettyInsignificant);
        }
    }
}