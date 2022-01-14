namespace Rebus.Exceptions;

/// <summary>
/// Marker interface for exceptions for which Rebus should not bother to retry delivery
/// </summary>
public interface IFailFastException
{
}