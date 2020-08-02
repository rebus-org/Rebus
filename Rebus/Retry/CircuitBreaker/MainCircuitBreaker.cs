using Rebus.Bus;
using Rebus.Logging;
using Rebus.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Retry.CircuitBreaker
{
    internal class MainCircuitBreaker : ICircuitBreaker, IInitializable, IDisposable
    {
        const string BackgroundTaskName = "CircuitBreakersResetTimer";

        readonly IAsyncTask _resetCircuitBreakerTask;

        readonly IList<ICircuitBreaker> _circuitBreakers;
        readonly RebusBus _rebusBus;
        readonly BusLifetimeEvents _busLifetimeEvents;
        readonly ILog _log;

        int _configuredNumberOfWorkers;

        bool _disposed;

        public MainCircuitBreaker(IList<ICircuitBreaker> circuitBreakers
            , IRebusLoggerFactory rebusLoggerFactory
            , IAsyncTaskFactory asyncTaskFactory
            , RebusBus rebusBus
            , BusLifetimeEvents busLifetimeEvents)
        {
            _circuitBreakers = circuitBreakers ?? new List<ICircuitBreaker>();
            _log = rebusLoggerFactory?.GetLogger<MainCircuitBreaker>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _rebusBus = rebusBus;
            _busLifetimeEvents = busLifetimeEvents;

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

            _log.Info("Circuit breaker change from {PreviousState} to {State}", previousState, State);
            _busLifetimeEvents.RaiseCircuitBreakerChanged(State);

            if (IsClosed) 
            {
                _rebusBus.SetNumberOfWorkers(_configuredNumberOfWorkers);
                return;
            }
                

            if (IsHalfOpen) 
            {
                _rebusBus.SetNumberOfWorkers(1);
                return;
            }

            if (IsOpen)
            {
                _rebusBus.SetNumberOfWorkers(0);
                return;
            }
        }

        public async Task TryReset()
        {
            foreach (var circuitBreaker in _circuitBreakers)
                await circuitBreaker.TryReset();
        }

        public void Initialize()
        {
            _configuredNumberOfWorkers = _rebusBus.Advanced.Workers.Count;
            _resetCircuitBreakerTask.Start();
        }

        /// <summary>
        /// Last-resort disposal of reset circuit breaker reset timer
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _resetCircuitBreakerTask.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
