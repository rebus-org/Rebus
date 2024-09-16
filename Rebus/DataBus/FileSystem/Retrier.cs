using System;
using System.Threading;
using Rebus.Logging;

namespace Rebus.DataBus.FileSystem;

sealed class Retrier
{
    readonly ILog _log;

    public Retrier(IRebusLoggerFactory rebusLoggerFactory)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _log = rebusLoggerFactory.GetLogger<Retrier>();
    }

    public void Execute(Action action, Func<Exception, bool> handle, int attempts, int delaySeconds = 1)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (handle == null) throw new ArgumentNullException(nameof(handle));

        while (true)
        {
            try
            {
                action();
                return;
            }
            catch (Exception exception) when (handle(exception) && attempts-- > 0)
            {
                _log.Debug("Retrier caught exception {exception} - waiting {delaySeconds} before trying again", 
                    exception, delaySeconds);

                Thread.Sleep(delaySeconds * 1000);
            }
        }
    }
}