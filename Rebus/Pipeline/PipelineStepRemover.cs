using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Decorator of <see cref="IPipeline"/> that can remove steps based on a predicate
    /// </summary>
    public class PipelineStepRemover : IPipeline
    {
        readonly List<Func<IIncomingStep, bool>> _incomingStepPredicates = new List<Func<IIncomingStep, bool>>();
        readonly List<Func<IOutgoingStep, bool>> _outgoingStepPredicates = new List<Func<IOutgoingStep, bool>>();
        readonly IPipeline _pipeline;

        /// <summary>
        /// Constructs the pipeline step remover, wrapping the given pipeline
        /// </summary>
        public PipelineStepRemover(IPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        /// <summary>
        /// Gets the outgoing steps from the wrapped pipeline, unless those where one of the registered outgoing step predicates match
        /// </summary>
        public IEnumerable<IOutgoingStep> SendPipeline()
        {
            return _pipeline
                .SendPipeline()
                .Where(s => !_outgoingStepPredicates.Any(p => p(s)));
        }

        /// <summary>
        /// Gets the incoming steps from the wrapped pipeline, unless those where one of the registered incoming step predicates match
        /// </summary>
        public IEnumerable<IIncomingStep> ReceivePipeline()
        {
            return _pipeline
                .ReceivePipeline()
                .Where(s => !_incomingStepPredicates.Any(p => p(s)));
        }

        /// <summary>
        /// Adds the predicate, causing matching incoming steps to be removed from the pipeline
        /// </summary>
        public PipelineStepRemover RemoveIncomingStep(Func<IIncomingStep, bool> stepPredicate)
        {
            _incomingStepPredicates.Add(stepPredicate);
            return this;
        }

        /// <summary>
        /// Adds the predicate, causing matching outgoing steps to be removed from the pipeline
        /// </summary>
        public PipelineStepRemover RemoveOutgoingStep(Func<IOutgoingStep, bool> stepPredicate)
        {
            _outgoingStepPredicates.Add(stepPredicate);
            return this;
        }
    }
}