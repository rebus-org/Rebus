using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Decorator of <see cref="IPipeline"/> that can inject one or more steps into either pipeline,
    /// positionint the injected steps relatively to another step by its type.
    /// Could probably be extended with more ways of detecting "the other step" than by its concrete type.
    /// </summary>
    public class PipelineStepInjector : IPipeline
    {
        readonly ConcurrentDictionary<Type, List<Tuple<PipelineRelativePosition, IOutgoingStep>>> _outgoingInjectedSteps =
            new ConcurrentDictionary<Type, List<Tuple<PipelineRelativePosition, IOutgoingStep>>>();

        readonly ConcurrentDictionary<Type, List<Tuple<PipelineRelativePosition, IIncomingStep>>> _incomingInjectedSteps =
            new ConcurrentDictionary<Type, List<Tuple<PipelineRelativePosition, IIncomingStep>>>();

        readonly IPipeline _pipeline;

        /// <summary>
        /// Constructs the step injector, wrapping the given <see cref="IPipeline"/>
        /// </summary>
        public PipelineStepInjector(IPipeline pipeline)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            _pipeline = pipeline;
        }

        /// <summary>
        /// Gets the ordered sequence of <see cref="IOutgoingStep"/> that makes up the outgoing pipeline, injecting any configured outgoing steps
        /// at their appropriate places
        /// </summary>
        public IEnumerable<IOutgoingStep> SendPipeline()
        {
            var encounteredStepTypes = new HashSet<Type>();

            foreach (var step in _pipeline.SendPipeline())
            {
                var currentStepType = step.GetType();
                
                encounteredStepTypes.Add(currentStepType);
                
                List<Tuple<PipelineRelativePosition, IOutgoingStep>> injectedStep;

                if (_outgoingInjectedSteps.TryGetValue(currentStepType, out injectedStep))
                {
                    foreach (var stepToInject in injectedStep.Where(i => i.Item1 == PipelineRelativePosition.Before))
                    {
                        yield return stepToInject.Item2;
                    }

                    yield return step;

                    foreach (var stepToInject in injectedStep.Where(i => i.Item1 == PipelineRelativePosition.After))
                    {
                        yield return stepToInject.Item2;
                    }
                }
                else
                {
                    yield return step;
                }
            }

            var typesNotEncountered = _outgoingInjectedSteps.Keys.Except(encounteredStepTypes);

            foreach (var typeNotEncountered in typesNotEncountered)
            {
                foreach (var missingStep in _outgoingInjectedSteps[typeNotEncountered])
                {
                    yield return missingStep.Item2;
                }
            }
        }

        /// <summary>
        /// Gets the ordered sequence of <see cref="IIncomingStep"/> that makes up the incoming pipeline, injecting any configured incoming steps
        /// at their appropriate places
        /// </summary>
        public IEnumerable<IIncomingStep> ReceivePipeline()
        {
            var encounteredStepTypes = new HashSet<Type>();

            foreach (var step in _pipeline.ReceivePipeline())
            {
                var currentStepType = step.GetType();

                encounteredStepTypes.Add(currentStepType);

                List<Tuple<PipelineRelativePosition, IIncomingStep>> injectedStep;

                if (_incomingInjectedSteps.TryGetValue(currentStepType, out injectedStep))
                {
                    foreach (var stepToInject in injectedStep.Where(i => i.Item1 == PipelineRelativePosition.Before))
                    {
                        yield return stepToInject.Item2;
                    }

                    yield return step;

                    foreach (var stepToInject in injectedStep.Where(i => i.Item1 == PipelineRelativePosition.After))
                    {
                        yield return stepToInject.Item2;
                    }
                }
                else
                {
                    yield return step;
                }
            }

            var typesNotEncountered = _incomingInjectedSteps.Keys.Except(encounteredStepTypes);

            foreach (var typeNotEncountered in typesNotEncountered)
            {
                foreach (var missingStep in _incomingInjectedSteps[typeNotEncountered])
                {
                    yield return missingStep.Item2;
                }
            }
        }

        /// <summary>
        /// Configures injection of the given <see cref="IOutgoingStep"/>, positioning it relative to another step
        /// specified by <paramref name="anchorStep"/>. The relative position is specified with either
        /// <see cref="PipelineRelativePosition.Before"/> or <see cref="PipelineRelativePosition.After"/>
        /// </summary>
        public PipelineStepInjector OnSend(IOutgoingStep step, PipelineRelativePosition position, Type anchorStep)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (anchorStep == null) throw new ArgumentNullException(nameof(anchorStep));

            _outgoingInjectedSteps
                .GetOrAdd(anchorStep, _ => new List<Tuple<PipelineRelativePosition, IOutgoingStep>>())
                .Add(Tuple.Create(position, step));

            return this;
        }

        /// <summary>
        /// Configures injection of the given <see cref="IIncomingStep"/>, positioning it relative to another step
        /// specified by <paramref name="anchorStep"/>. The relative position is specified with either
        /// <see cref="PipelineRelativePosition.Before"/> or <see cref="PipelineRelativePosition.After"/>
        /// </summary>
        public PipelineStepInjector OnReceive(IIncomingStep step, PipelineRelativePosition position, Type anchorStep)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (anchorStep == null) throw new ArgumentNullException(nameof(anchorStep));

            _incomingInjectedSteps
                .GetOrAdd(anchorStep, _ => new List<Tuple<PipelineRelativePosition, IIncomingStep>>())
                .Add(Tuple.Create(position, step));

            return this;
        }
    }
}