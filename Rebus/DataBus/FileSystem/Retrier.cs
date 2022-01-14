using System;
using System.Threading;
using Rebus.Logging;

namespace Rebus.DataBus.FileSystem;

class Retrier
{
    readonly ILog _log;

    public Retrier(IRebusLoggerFactory rebusLoggerFactory) => _log = rebusLoggerFactory.GetLogger<Retrier>();

    public void Execute(Action action, Func<Exception, bool> handle, int attempts, int delaySeconds = 1)
    {
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