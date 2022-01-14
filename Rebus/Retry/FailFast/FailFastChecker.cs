using Rebus.Exceptions;
using System;

namespace Rebus.Retry.FailFast;

/// <summary>
/// Implementation of <seealso cref="IFailFastChecker"/> that determines that if an exception is
/// <seealso cref=" FailFastException"/>, it should fail fast. Children of this class could
/// further define additional logic to check if a message with exception should fail fast.
/// </summary>
public class FailFastChecker : IFailFastChecker
{
    /// <summary>
    /// Checks if a message with exception should fail fast
    /// </summary>
    public virtual bool ShouldFailFast(string messageId, Exception exception) => exception is IFailFastException;
}