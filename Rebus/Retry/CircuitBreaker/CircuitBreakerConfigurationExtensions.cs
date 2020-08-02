using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Threading;
using Rebus.Time;
using System;
using System.Collections.Generic;

namespace Rebus.Retry.CircuitBreaker
{
    /// <summary>
    /// Configuration extensions for the Circuit breakers
    /// </summary>
    public static class CircuitBreakerConfigurationExtensions
    {
        /// <summary>
        /// Enabling fluent configuration of circuit breakers
        /// </summary>
        /// <param name="optionsConfigurer"></param>
        /// <param name="circuitBreakerBuilder"></param>
        public static void SetCircuitBreakers(this OptionsConfigurer optionsConfigurer
            , Action<CircuitBreakerConfigurationBuilder> circuitBreakerBuilder) 
        {
            var builder = new CircuitBreakerConfigurationBuilder();
            circuitBreakerBuilder?.Invoke(builder);
            var circuitBreakers = builder.Build();

            optionsConfigurer.Decorate<IErrorHandler>(c => 
            {
                var innerHandler = c.Get<IErrorHandler>();
                var loggerFactory = c.Get<IRebusLoggerFactory>();
                var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                var rebusBus = c.Get<RebusBus>();
                var circuitBreaker = new MainCircuitBreaker(circuitBreakers, loggerFactory, asyncTaskFactory, rebusBus);
                optionsConfigurer.Register<IInitializable>(x => circuitBreaker);

                return new CircuitBreakerErrorHandler(circuitBreaker, innerHandler, loggerFactory);
            });
        }

        /// <summary>
        /// Configuration builder to fluently register circuit breakers
        /// </summary>
        public class CircuitBreakerConfigurationBuilder
        {

            private readonly IList<ICircuitBreaker> _circuitBreakerStores;

            internal CircuitBreakerConfigurationBuilder()
            {
                _circuitBreakerStores = new List<ICircuitBreaker>();
            }

            /// <summary>
            /// Register a circuit breaker based on an <typeparamref name="TException"/>
            /// </summary>
            /// <typeparam name="TException"></typeparam>
            public CircuitBreakerConfigurationBuilder OpenOn<TException>(
                int attempts = CircuitBreakerSettings.DefaultAttempts
                , int trackingPeriodInSeconds = CircuitBreakerSettings.DefaultTrackingPeriodInSeconds
                , int resetIntervalInSeconds = CircuitBreakerSettings.DefaultResetIntervalInSeconds)
                where TException : Exception
            {
                var settings = new CircuitBreakerSettings(attempts, trackingPeriodInSeconds, resetIntervalInSeconds);
                _circuitBreakerStores.Add(new ExceptionTypeCircuitBreaker(typeof(TException), settings, new DefaultRebusTime()));
                return this;
            }

            internal IList<ICircuitBreaker> Build() 
            {
                return _circuitBreakerStores;
            }
        }   
    }
}
