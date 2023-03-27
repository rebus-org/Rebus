using Rebus.Pipeline;

namespace Rebus.Retry.Simple;

/// <summary>
/// Marker interface for the retry step
/// </summary>
public interface IRetryStep : IIncomingStep
{
}