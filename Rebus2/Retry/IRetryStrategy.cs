using Rebus2.Pipeline;

namespace Rebus2.Retry
{
    public interface IRetryStrategy
    {
        IStep GetRetryStep();
    }
}