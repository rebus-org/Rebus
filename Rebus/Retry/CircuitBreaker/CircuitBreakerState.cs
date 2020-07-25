namespace Rebus.Retry.CircuitBreaker
{
    internal enum CircuitBreakerState
    {
        Closed = 0,
        HalfOpen = 1,
        Open = 2
    }
}
