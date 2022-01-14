using System.Threading.Tasks;

namespace Rebus.Pipeline;

/// <summary>
/// The invoker is capable of invoking an ordered pipeline of steps
/// </summary>
public interface IPipelineInvoker
{
    /// <summary>
    /// Invokes the pipeline of <see cref="IIncomingStep"/> steps, passing the given <see cref="IncomingStepContext"/> to each step as it is invoked
    /// </summary>
    Task Invoke(IncomingStepContext context);

    /// <summary>
    /// Invokes the pipeline of <see cref="IOutgoingStep"/> steps, passing the given <see cref="OutgoingStepContext"/> to each step as it is invoked
    /// </summary>
    Task Invoke(OutgoingStepContext context);
}