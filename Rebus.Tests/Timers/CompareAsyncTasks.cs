using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Threading;
#pragma warning disable 1998

namespace Rebus.Tests.Timers
{
    [Ignore, TestFixture(typeof(TplTaskFactory))]
    public class CompareAsyncTasks<TTaskFactory> : FixtureBase where TTaskFactory : IAsyncTaskFactory, new()
    {
        TTaskFactory _factory;

        protected override void SetUp()
        {
            _factory = new TTaskFactory();
        }

        [Test]
        public async Task NizzleName()
        {
            var task = _factory.CreateTask(TimeSpan.FromSeconds(0.1), async () =>
            {
                var now = DateTime.Now;

                Console.WriteLine($"{now:HH:mm:ss tt}");
            });

            using (task)
            {
                task.Start();

                await Task.Delay(20000);
            }
        }
    }

    public class TplTaskFactory : IAsyncTaskFactory
    {
        public IAsyncTask CreateTask(TimeSpan interval, Func<Task> action)
        {
            var asyncTask = new AsyncTask("task", action, new ConsoleLoggerFactory(false));
            asyncTask.Interval = interval;
            return asyncTask;
        }
    }

    public interface IAsyncTaskFactory
    {
        IAsyncTask CreateTask(TimeSpan interval, Func<Task> action);
    }
}