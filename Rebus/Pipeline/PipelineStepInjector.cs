using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Pipeline;

/// <summary>
/// Decorator of <see cref="IPipeline"/> that can inject one or more steps into either pipeline,
/// position in the injected steps relatively to another step by its type.
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
    public IOutgoingStep[] SendPipeline()
    {
        return ComposeSendPipeline().ToArray();
    }

    /// <summary>
    /// Gets the ordered sequence of <see cref="IIncomingStep"/> that makes up the incoming pipeline, injecting any configured incoming steps
    /// at their appropriate places
    /// </summary>
    public IIncomingStep[] ReceivePipeline()
    {
        return ComposeReceivePipeline().ToArray();
    }

    IEnumerable<IIncomingStep> ComposeReceivePipeline()
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

        var errors = typesNotEncountered
            .Select(type => new
            {
                MissingStepType = type,
                StepsToInject = _incomingInjectedSteps[type]
            })
            .SelectMany(a => a.StepsToInject.Select(s => new
            {
                a.MissingStepType,
                StepToInject = s.Item2,
                Position = s.Item1
            }))
            .Select(a => $"    {a.StepToInject} => {a.Position} => {a.MissingStepType}")
            .ToList();

        if (errors.Any())
        {
            throw new ArgumentException(
                $@"Could not finish composition of the RECEIVE pipeline because the following injections could not be made:

{string.Join(Environment.NewLine, errors)}

Please pick other steps to use when anchoring your step injections, or pick another way of assembling the pipeline.
If you require the ultimate flexibility, you will probably need to decorate IPipeline and compose it manually.
");
        }
    }

    IEnumerable<IOutgoingStep> ComposeSendPipeline()
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

        var errors = typesNotEncountered
            .Select(type => new
            {
                MissingStepType = type,
                StepsToInject = _outgoingInjectedSteps[type]
            })
            .SelectMany(a => a.StepsToInject.Select(s => new
            {
                a.MissingStepType,
                StepToInject = s.Item2,
                Position = s.Item1
            }))
            .Select(a => $"    {a.StepToInject} => {a.Position} => {a.MissingStepType}")
            .ToList();

        if (errors.Any())
        {
            throw new ArgumentException(
                $@"Could not finish composition of the SEND pipeline because the following injections could not be made:

{string.Join(Environment.NewLine, errors)}

Please pick other steps to use when anchoring your step injections, or pick another way of assembling the pipeline.
If you require the ultimate flexibility, you will probably need to decorate IPipeline and compose it manually.
");
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