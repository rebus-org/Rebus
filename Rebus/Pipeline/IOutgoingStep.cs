using System;
using System.Threading.Tasks;

namespace Rebus.Pipeline;

/// <summary>
/// Represents a step that will have its <see cref="Process"/> method called for each outgoing message to be sent.
/// </summary>
public interface IOutgoingStep : IStep
{
    /// <summary>
    /// Carries out whichever logic it takes to do something good for the outgoing message :)
    /// </summary>
    Task Process(OutgoingStepContext context, Func<Task> next);
}