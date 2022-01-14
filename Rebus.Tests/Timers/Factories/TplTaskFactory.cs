using System;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Threading;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.Tests.Timers.Factories;

public class TplTaskFactory : IAsyncTaskFactory
{
    public IAsyncTask CreateTask(TimeSpan interval, Func<Task> action)
    {
        var asyncTask = new TplAsyncTask("task", action, new ConsoleLoggerFactory(false), false)
        {
            Interval = interval
        };

        return asyncTask;
    }
}