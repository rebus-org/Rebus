using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Step injector that may be promoted to become the default pipeline step injector
    /// </summary>
    public class PipelineStepInjector : IPipeline
    {
        readonly ConcurrentDictionary<Type, Tuple<PipelineRelativePosition, IOutgoingStep>> _outgoingInjectedSteps = new ConcurrentDictionary<Type, Tuple<PipelineRelativePosition, IOutgoingStep>>();
        readonly ConcurrentDictionary<Type, Tuple<PipelineRelativePosition, IIncomingStep>> _incomingInjectedSteps = new ConcurrentDictionary<Type, Tuple<PipelineRelativePosition, IIncomingStep>>();
        readonly IPipeline _pipeline;

        /// <summary>
        /// Constructs the step injector, wrapping the given <see cref="IPipeline"/>
        /// </summary>
        public PipelineStepInjector(IPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public IEnumerable<IOutgoingStep> SendPipeline()
        {
            foreach (var step in _pipeline.SendPipeline())
            {
                Tuple<PipelineRelativePosition, IOutgoingStep> injectedStep;

                if (_outgoingInjectedSteps.TryGetValue(step.GetType(), out injectedStep))
                {
                    if (injectedStep.Item1 == PipelineRelativePosition.Before)
                    {
                        yield return injectedStep.Item2;
                        yield return step;
                    }
                    else
                    {
                        yield return step;
                        yield return injectedStep.Item2;
                    }
                }
                else
                {
                    yield return step;
                }
            }
        }

        public IEnumerable<IIncomingStep> ReceivePipeline()
        {
            foreach (var step in _pipeline.ReceivePipeline())
            {
                Tuple<PipelineRelativePosition, IIncomingStep> injectedStep;

                if (_incomingInjectedSteps.TryGetValue(step.GetType(), out injectedStep))
                {
                    if (injectedStep.Item1 == PipelineRelativePosition.Before)
                    {
                        yield return injectedStep.Item2;
                        yield return step;
                    }
                    else
                    {
                        yield return step;
                        yield return injectedStep.Item2;
                    }
                }
                else
                {
                    yield return step;
                }
            }
        }

        public PipelineStepInjector OnSend(IOutgoingStep step, PipelineRelativePosition position, Type anchorStep)
        {
            _outgoingInjectedSteps[anchorStep] = Tuple.Create(position, step);
            return this;
        }

        public PipelineStepInjector OnReceive(IIncomingStep step, PipelineRelativePosition position, Type anchorStep)
        {
            _incomingInjectedSteps[anchorStep] = Tuple.Create(position, step);
            return this;
        }
    }
}