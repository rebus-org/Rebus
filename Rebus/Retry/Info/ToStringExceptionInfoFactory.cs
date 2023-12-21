using System;

namespace Rebus.Retry.Info;

/// <summary>
/// Creates <see cref="ExceptionInfo"/>s using a simple ToString() method.
/// </summary>
public class ToStringExceptionInfoFactory : IExceptionInfoFactory
{
    /// <summary>
    /// Create an <see cref="ExceptionInfo"/> from <see cref="Exception.ToString()"/>.
    /// </summary>
    /// <param name="exception">Source exception.</param>
    /// <returns>An <see cref="ExceptionInfo"/> containing information from the supplied exception.</returns>
    public ExceptionInfo CreateInfo(Exception exception) => ExceptionInfo.FromException(exception);
}