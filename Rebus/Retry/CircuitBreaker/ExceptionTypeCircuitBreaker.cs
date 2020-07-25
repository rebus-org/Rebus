using System;
using System.Linq;

namespace Rebus.Retry.CircuitBreaker
{

    internal class ExceptionTypeCircuitBreaker : ICircuitBreaker
    {
        private readonly Type exceptionType;
        private readonly CircuitBreakerSettings settings;
        private DateTime lastTrip;
        private volatile int trips;


        public ExceptionTypeCircuitBreaker(Type exceptionType, CircuitBreakerSettings settings)
        {
            this.exceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            
            State = CircuitBreakerState.Closed;
            lastTrip = DateTime.Now;
        }

        public CircuitBreakerState State { get; private set; }

        public bool IsClosed => State == CircuitBreakerState.Closed;

        public bool IsHalfOpen => State == CircuitBreakerState.HalfOpen;

        public bool IsOpen => State == CircuitBreakerState.Open;

        public void Trip(Exception exception)
        {
            if (ShouldTripCircuitBreaker(exception) == false)
                return;

            // Do the tripping
            var lastTrip = this.lastTrip;
            var currentState = State;

            Trip();

            var timeSinceLastTrip = (DateTime.Now  - lastTrip);
            if (timeSinceLastTrip < settings.TrackingPeriod)
            {
                if (trips >= settings.Attempts)
                {
                    State = CircuitBreakerState.Open;
                }
            }
            else 
            {
                trips = 0;
            }
        }

        private bool ShouldTripCircuitBreaker(Exception exception) 
        {
            if (exception is AggregateException e)
            {
                var actualException = e.InnerExceptions.First();
                if (actualException.GetType().Equals(exceptionType))
                    return true;
            }

            if (exception.GetType().Equals(exceptionType))
                return true;

            return false;
        }

        private void Trip() 
        {
            trips++;
            lastTrip = DateTime.Now;
        }
    }
} 
