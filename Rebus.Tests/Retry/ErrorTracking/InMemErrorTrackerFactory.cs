using Rebus.Logging;
using Rebus.Retry;
using Rebus.Retry.ErrorTracking;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts.Errors;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Time;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.Tests.Retry.ErrorTracking;

public class InMemErrorTrackerFactory : IErrorTrackerFactory
{
    readonly ConsoleLoggerFactory _consoleLoggerFactory = new ConsoleLoggerFactory(false);

    public IErrorTracker Create(RetryStrategySettings settings)
    {
        return new InMemErrorTracker(settings, _consoleLoggerFactory, new TplAsyncTaskFactory(_consoleLoggerFactory), new FakeRebusTime());
    }

    public void Dispose()
    {
    }
}