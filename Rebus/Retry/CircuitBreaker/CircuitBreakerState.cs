namespace Rebus.Retry.CircuitBreaker
{
    /// <summary>
    /// Describes the state of the circuit breaker
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// State describing that the circuit is <see cref="Closed"/>, meaning that Rebus is working at the configured set of workers
        /// </summary>
        Closed = 0,

        /// <summary>
        /// State describing that the circuit is <see cref="HalfOpen"/>, meaning that Rebus is working at a reduced set of workers
        /// </summary>
        HalfOpen = 1,

        /// <summary>
        /// State describing that the circuit is <see cref="Open"/>, meaning that Rebus has reduced the number of workers until the faulty 
        /// circuit breaker is reset
        /// </summary>
        Open = 2
    }
}
