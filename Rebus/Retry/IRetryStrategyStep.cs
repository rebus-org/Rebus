using Rebus.Pipeline;

namespace Rebus.Retry;

/// <summary>
/// Special marker for the retry strategy step
/// </summary>
public interface IRetryStrategyStep : IIncomingStep
{
}