using System;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Threading;
using Rebus.Threading.SystemThreadingTimer;

namespace Rebus.Tests.Timers.Factories;

public class ThreadingTimerTaskFactory : IAsyncTaskFactory
{
    public IAsyncTask CreateTask(TimeSpan interval, Func<Task> action)
    {
        var asyncTask = new SystemThreadingTimerAsyncTask("task", action, new ConsoleLoggerFactory(false), false)
        {
            Interval = interval
        };

        return asyncTask;
    }
}