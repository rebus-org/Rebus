using System;
using System.Threading.Tasks;

namespace Rebus.Retry.CircuitBreaker
{
    internal interface ICircuitBreaker
    {
        CircuitBreakerState State { get; }
        bool IsClosed { get; }
        bool IsHalfOpen { get; }
        bool IsOpen { get; }
        void Trip(Exception exception);

        Task TryReset();
    }
}
