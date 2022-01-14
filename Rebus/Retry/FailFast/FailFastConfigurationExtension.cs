using System;
using Rebus.Config;

namespace Rebus.Retry.FailFast;

/// <summary>
/// Extension methods for helping with configuration fail-fast behavior
/// </summary>
public static class FailFastConfigurationExtension
{
    /// <summary>
    /// Decorates the current <see cref="IFailFastChecker"/> with a filter that causes Rebus to fail fast on exceptions of type
    /// <typeparamref name="TException"/> (optionally also requiring it to satisfy the when <paramref name="when"/>)
    /// </summary>
    public static void FailFastOn<TException>(this OptionsConfigurer configurer, Func<TException, bool> when = null) where TException : Exception
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer.Decorate<IFailFastChecker>(c =>
        {
            var failFastChecker = c.Get<IFailFastChecker>();

            return new FailFastOnSpecificExceptionTypeAndPredicate<TException>(failFastChecker, when);
        });
    }

    class FailFastOnSpecificExceptionTypeAndPredicate<TException> : IFailFastChecker where TException : Exception
    {
        readonly IFailFastChecker _failFastChecker;
        readonly Func<TException, bool> _predicate;

        public FailFastOnSpecificExceptionTypeAndPredicate(IFailFastChecker failFastChecker, Func<TException, bool> predicate = null)
        {
            _failFastChecker = failFastChecker ?? throw new ArgumentNullException(nameof(failFastChecker));
            _predicate = predicate ?? (_ => true);
        }

        public bool ShouldFailFast(string messageId, Exception exception)
        {
            return exception is TException specificException && _predicate(specificException)
                   || _failFastChecker.ShouldFailFast(messageId, exception);
        }
    }
}