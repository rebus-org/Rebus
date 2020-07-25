using Rebus.Bus;
using Rebus.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Retry.CircuitBreaker
{
    internal class MainCircuitBreaker : ICircuitBreaker
    {
        private readonly IList<ICircuitBreaker> circuitBreakers;
        private readonly RebusBus rebusBus;
        private readonly ILog _log;

        public MainCircuitBreaker(IList<ICircuitBreaker> circuitBreakers, IRebusLoggerFactory rebusLoggerFactory, RebusBus rebusBus)
        {
            this.circuitBreakers = circuitBreakers ?? throw new ArgumentNullException(nameof(circuitBreakers));
            this.rebusBus = rebusBus;
            _log = rebusLoggerFactory?.GetLogger<MainCircuitBreaker>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
        }

        public CircuitBreakerState State => circuitBreakers.Aggregate(CircuitBreakerState.Closed, (currentState, incomming) =>
        {
            if (incomming.State > currentState)
                return incomming.State;

            return currentState;
        });

        public bool IsClosed => circuitBreakers.All(x => x.State == CircuitBreakerState.Closed);

        public bool IsHalfOpen => State == CircuitBreakerState.HalfOpen;

        public bool IsOpen => State == CircuitBreakerState.Open;

        public void Trip(Exception exception)
        {
            var previousState = State;

            foreach (var circuitBreaker in circuitBreakers)
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
                rebusBus.SetNumberOfWorkers(0);
            }
        }
    }
}
