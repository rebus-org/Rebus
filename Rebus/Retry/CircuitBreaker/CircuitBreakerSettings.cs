using System;

namespace Rebus.Retry.CircuitBreaker
{
    /// <summary>
    /// Contains the settings used by <see cref="CircuitBreakerSettings"/>
    /// </summary>
    public class CircuitBreakerSettings
    {
        /// <summary>
        /// Default Attempts a circuit breaker will fail within a given <see cref="TrackingPeriod"/>
        /// </summary>
        public const int DefaultAttempts = 5;

        /// <summary>
        /// Default period where in errors are getting tracked
        /// </summary>
        public const int DefaultTrackingPeriodInSeconds = 30;

        /// <summary>
        /// Defailt time Interval for when the circuit breaker will close after being opened 
        /// </summary>
        public const int DefaultResetIntervalInSeconds = 300;

        /// <summary>
        /// Number of attempts that the circuit breaker will fail within a given <see cref="TrackingPeriod"/>
        /// </summary>
        public int Attempts { get; private set; }

        /// <summary>
        /// Time window wherein consecutive errors are getting tracked
        /// </summary>
        public TimeSpan TrackingPeriod { get; private set; }

        /// <summary>
        /// Time Interval for when the circuit breaker will close after being opened
        /// </summary>
        public TimeSpan ResetInterval { get; private set; }

        /// <summary>
        /// Create a setting for a given circuit breaker
        /// </summary>
        public CircuitBreakerSettings(int attempts, int trackingPeriodInSeconds, int resetIntervalInSeconds)
        {
            Attempts = attempts;
            TrackingPeriod = TimeSpan.FromSeconds(trackingPeriodInSeconds);
            ResetInterval = TimeSpan.FromSeconds(resetIntervalInSeconds);
        }
    }
}
