using System;
using Rebus.Logging;

namespace Rebus.Retry.Simple;

class DefaultExceptionLogger : IExceptionLogger
{
    readonly ILog _log;

    public DefaultExceptionLogger(IRebusLoggerFactory rebusLoggerFactory)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _log = rebusLoggerFactory.GetLogger<DefaultExceptionLogger>();
    }

    public void LogException(string messageId, Exception exception, int errorCount, bool isFinal) =>
        _log.Warn(
            exception: exception,
            message: isFinal
                ? "Unhandled exception {errorNumber} (FINAL) while handling message with ID {messageId}"
                : "Unhandled exception {errorNumber} while handling message with ID {messageId}",
            objs: new object[] { errorCount, messageId }
        );
}