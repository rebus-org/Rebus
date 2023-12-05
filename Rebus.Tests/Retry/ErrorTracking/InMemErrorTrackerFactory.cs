using Rebus.Logging;
using Rebus.Retry;
using Rebus.Retry.ErrorTracking;
using Rebus.Retry.Info;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts.Errors;
using Rebus.Tests.Time;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.Tests.Retry.ErrorTracking;

public class InMemErrorTrackerFactory : IErrorTrackerFactory
{
    public IErrorTracker Create(RetryStrategySettings settings, IExceptionLogger exceptionLogger)
    {
        return new InMemErrorTracker(settings, new TplAsyncTaskFactory(new ConsoleLoggerFactory(colored: false)), new FakeRebusTime(), exceptionLogger, new ToStringExceptionInfoFactory());
    }

    public void Dispose()
    {
    }
}