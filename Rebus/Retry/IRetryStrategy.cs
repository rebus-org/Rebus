using Rebus.Pipeline;

namespace Rebus.Retry
{
    public interface IRetryStrategy
    {
        IRetryStrategyStep GetRetryStep();
    }

    public interface IRetryStrategyStep : IIncomingStep
    {
    }
}