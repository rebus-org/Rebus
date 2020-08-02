using Rebus.Time;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Retry.CircuitBreaker
{

    internal class ExceptionTypeCircuitBreaker : ICircuitBreaker
    {
        private readonly Type exceptionType;
        private readonly CircuitBreakerSettings settings;
        private readonly IRebusTime rebusTime;
        private ConcurrentDictionary<long, DateTimeOffset> _errorDates;

        public ExceptionTypeCircuitBreaker(Type exceptionType
            , CircuitBreakerSettings settings
            , IRebusTime rebusTime)
        {
            this.exceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.rebusTime = rebusTime;
            State = CircuitBreakerState.Closed;

            _errorDates = new ConcurrentDictionary<long, DateTimeOffset>();
        }

        public CircuitBreakerState State { get; private set; }

        public bool IsClosed => State == CircuitBreakerState.Closed;

        public bool IsHalfOpen => State == CircuitBreakerState.HalfOpen;

        public bool IsOpen => State == CircuitBreakerState.Open;

        public void Trip(Exception exception)
        {
            if (ShouldTripCircuitBreaker(exception) == false)
                return;

            var timeStamp = rebusTime.Now;
            _errorDates.TryAdd(timeStamp.Ticks, timeStamp);

            var errorsInPeriod = _errorDates
                .Where(x => x.Key > timeStamp.Ticks - settings.TrackingPeriod.Ticks)
                .Take(settings.Attempts);

            // Do the tripping
            if (errorsInPeriod.Count() >= settings.Attempts)
            {
                State = CircuitBreakerState.Open;
            }

            RemoveOutOfPeriodErrors(errorsInPeriod);
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

        private void RemoveOutOfPeriodErrors(IEnumerable<KeyValuePair<long, DateTimeOffset>> tripsInPeriod)
        {
            var outDatedTimeStamps = _errorDates
                .Except(tripsInPeriod)
                .ToList();

            foreach(var outDatedTimeStamp in outDatedTimeStamps) 
                _errorDates.TryRemove(outDatedTimeStamp.Key, out _);
        }

        public async Task Reset()
        {

            // Missing .net 4.5 compatibility 
            // await Task.CompletedTask;
            await Task.FromResult(0);

        }
    }
} 
