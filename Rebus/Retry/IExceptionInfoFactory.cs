using System;

namespace Rebus.Retry;

/// <summary>
/// An interface to handle the creation of portable, serializable, trackable <see cref="ExceptionInfo"/>s.
/// </summary>
public interface IExceptionInfoFactory
{
    /// <summary>
    /// Create an <see cref="ExceptionInfo"/> from an <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">Source exception.</param>
    /// <returns>An <see cref="ExceptionInfo"/> containing information from the supplied exception.</returns>
    ExceptionInfo CreateInfo(Exception exception);
}