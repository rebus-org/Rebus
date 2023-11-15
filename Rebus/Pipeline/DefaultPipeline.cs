using System.Collections.Generic;

namespace Rebus.Pipeline;

/// <summary>
/// Default pipeline implementation that can be built with a fluent syntax by calling <see cref="OnSend"/> and <see cref="OnReceive"/> respectively,
/// in the order that the steps must be invoked in.
/// </summary>
public class DefaultPipeline : IPipeline
{
    readonly List<IOutgoingStep> _sendSteps = new();
    readonly List<IIncomingStep> _receiveSteps = new();

    /// <summary>
    /// Creates the pipeline, possibly initializing it from the given <paramref name="initialOutgoingSteps"/> and/or <paramref name="initialIncomingSteps"/>
    /// </summary>
    public DefaultPipeline(IEnumerable<IOutgoingStep> initialOutgoingSteps = null, IEnumerable<IIncomingStep> initialIncomingSteps = null)
    {
        if (initialOutgoingSteps != null)
        {
            _sendSteps.AddRange(initialOutgoingSteps);
        }

        if (initialIncomingSteps != null)
        {
            _receiveSteps.AddRange(initialIncomingSteps);
        }
    }

    /// <summary>
    /// Gets the send pipeline
    /// </summary>
    public IOutgoingStep[] SendPipeline()
    {
        return _sendSteps.ToArray();
    }

    /// <summary>
    /// Gets the receive pipeline
    /// </summary>
    public IIncomingStep[] ReceivePipeline()
    {
        return _receiveSteps.ToArray();
    }

    /// <summary>
    /// Adds a new incoming step to the receive pipeline
    /// </summary>
    public DefaultPipeline OnReceive(IIncomingStep step)
    {
        _receiveSteps.Add(step);
        return this;
    }

    /// <summary>
    /// Adds a new outgoing step to the send pipeline
    /// </summary>
    public DefaultPipeline OnSend(IOutgoingStep step)
    {
        _sendSteps.Add(step);
        return this;
    }
}