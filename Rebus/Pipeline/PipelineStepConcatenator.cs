using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Pipeline;

/// <summary>
/// Decorator of <see cref="IPipeline"/> that can concatenate steps to either pipeline at the front or at the back
/// </summary>
public class PipelineStepConcatenator : IPipeline
{
    readonly List<IIncomingStep> _incomingFrontSteps = new();
    readonly List<IIncomingStep> _incomingBackSteps = new();
    readonly List<IOutgoingStep> _outgoingFrontSteps = new();
    readonly List<IOutgoingStep> _outgoingBackSteps = new();
    readonly IPipeline _pipeline;

    /// <summary>
    /// Constructs the concatenator
    /// </summary>
    /// <param name="pipeline"></param>
    public PipelineStepConcatenator(IPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    /// <summary>
    /// Sets the specified outgoing <paramref name="step"/> to be concatenated at the position specified by <paramref name="position"/>
    /// </summary>
    public PipelineStepConcatenator OnSend(IOutgoingStep step, PipelineAbsolutePosition position)
    {
        if (step == null) throw new ArgumentNullException(nameof(step));
        if (position == PipelineAbsolutePosition.Front)
        {
            _outgoingFrontSteps.Add(step);
        }
        else
        {
            _outgoingBackSteps.Add(step);
        }
        return this;
    }

    /// <summary>
    /// Sets the specified receive <paramref name="step"/> to be concatenated at the position specified by <paramref name="position"/>
    /// </summary>
    public PipelineStepConcatenator OnReceive(IIncomingStep step, PipelineAbsolutePosition position)
    {
        if (step == null) throw new ArgumentNullException(nameof(step));
        if (position == PipelineAbsolutePosition.Front)
        {
            _incomingFrontSteps.Add(step);
        }
        else
        {
            _incomingBackSteps.Add(step);
        }
        return this;
    }

    /// <summary>
    /// Gets the send pipeline with front and back steps concatenated
    /// </summary>
    public IOutgoingStep[] SendPipeline()
    {
        return _outgoingFrontSteps
            .Concat(_pipeline.SendPipeline())
            .Concat(_outgoingBackSteps)
            .ToArray();
    }

    /// <summary>
    /// Gets the receive pipeline with front and back steps concatenated
    /// </summary>
    public IIncomingStep[] ReceivePipeline()
    {
        return _incomingFrontSteps
            .Concat(_pipeline.ReceivePipeline())
            .Concat(_incomingBackSteps)
            .ToArray();
    }
}