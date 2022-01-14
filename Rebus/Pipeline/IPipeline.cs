namespace Rebus.Pipeline;

/// <summary>
/// Models a pipeline of steps that will be executed for each sent/received message respectively
/// </summary>
public interface IPipeline
{
    /// <summary>
    /// Gets the send pipeline, i.e. the sequence of <see cref="IOutgoingStep"/> implementations that will be executed for each outgoing message
    /// </summary>
    IOutgoingStep[] SendPipeline();

    /// <summary>
    /// Gets the receive pipeline, i.e. the sequence of <see cref="IIncomingStep"/> implementations that will be executed for each incoming message
    /// </summary>
    IIncomingStep[] ReceivePipeline();
}