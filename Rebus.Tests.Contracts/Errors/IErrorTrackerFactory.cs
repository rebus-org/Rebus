using System;
using Rebus.Retry;
using Rebus.Retry.Simple;

namespace Rebus.Tests.Contracts.Errors;

/// <summary>
/// Factory for <see cref="IErrorTracker"/> contract tests
/// </summary>
public interface IErrorTrackerFactory : IDisposable
{
    /// <summary>
    /// Should create a new error tracker instance
    /// </summary>
    IErrorTracker Create(RetryStrategySettings settings);
}