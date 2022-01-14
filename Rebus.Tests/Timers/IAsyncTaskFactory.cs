using System;
using System.Threading.Tasks;
using Rebus.Threading;

namespace Rebus.Tests.Timers;

public interface IAsyncTaskFactory
{
    IAsyncTask CreateTask(TimeSpan interval, Func<Task> action);
}