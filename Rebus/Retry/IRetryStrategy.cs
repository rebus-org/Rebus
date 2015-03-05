using Rebus.Pipeline;

namespace Rebus.Retry
{
    public interface IRetryStrategy
    {
        IIncomingStep GetRetryStep();
    }
}