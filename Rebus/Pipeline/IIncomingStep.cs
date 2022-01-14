using System;
using System.Threading.Tasks;

namespace Rebus.Pipeline;

/// <summary>
/// Represents a step that will have its <see cref="Process"/> method called for each incoming message to be handled.
/// </summary>
public interface IIncomingStep : IStep
{
    /// <summary>
    /// Carries out whichever logic it takes to do something good for the incoming message :)
    /// </summary>
    Task Process(IncomingStepContext context, Func<Task> next);
}