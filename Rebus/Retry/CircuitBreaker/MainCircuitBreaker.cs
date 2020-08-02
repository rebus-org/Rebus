using Rebus.Bus;
using Rebus.Logging;
using Rebus.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Retry.CircuitBreaker
{
    internal class MainCircuitBreaker : ICircuitBreaker
    {
        const string BackgroundTaskName = "CircuitBreakersResetTimer";

        readonly IAsyncTask _resetCircuitBreakerTask;

        readonly IList<ICircuitBreaker> _circuitBreakers;
        readonly RebusBus _rebusBus;
        readonly ILog _log;

        public MainCircuitBreaker(IList<ICircuitBreaker> circuitBreakers
            , IRebusLoggerFactory rebusLoggerFactory
            , IAsyncTaskFactory asyncTaskFactory
            , RebusBus rebusBus)
        {
            _circuitBreakers = circuitBreakers ?? throw new ArgumentNullException(nameof(circuitBreakers));
            _rebusBus = rebusBus;
            _log = rebusLoggerFactory?.GetLogger<MainCircuitBreaker>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));

            _resetCircuitBreakerTask = asyncTaskFactory.Create(
                BackgroundTaskName
                , TryReset
                , prettyInsignificant: false
                , intervalSeconds: 5);
        }

        public CircuitBreakerState State => _circuitBreakers.Aggregate(CircuitBreakerState.Closed, (currentState, incomming) =>
        {
            if (incomming.State > currentState)
                return incomming.State;

            return currentState;
        });

        public bool IsClosed => _circuitBreakers.All(x => x.State == CircuitBreakerState.Closed);

        public bool IsHalfOpen => State == CircuitBreakerState.HalfOpen;

        public bool IsOpen => State == CircuitBreakerState.Open;

        public void Trip(Exception exception)
        {
            var previousState = State;

            foreach (var circuitBreaker in _circuitBreakers)
                circuitBreaker.Trip(exception);

            if (previousState == State)
                return;

            // It Seems like state have changed?
            // Fire event?

            if (IsClosed)
                return;

            if (IsHalfOpen) 
            {
                // TODO: Pick a half open strategy
            }

            if (IsOpen)
            {
                _rebusBus.SetNumberOfWorkers(0);
            }
        }

        public async Task TryReset()
        {
            foreach (var circuitBreaker in _circuitBreakers)
                await circuitBreaker.TryReset();
        }
    }
}
