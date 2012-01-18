using System;
using System.Reflection;
using Topshelf;
using log4net;
using log4net.Config;

namespace Rebus.Timeout
{
    class Program
    {
        static void Main()
        {
            XmlConfigurator.Configure();

            HostFactory
                .Run(s =>
                         {
                             var text = "Rebus Timeout Service";
                             s.SetDescription(text);
                             s.SetDisplayName(text);
                             s.SetInstanceName("rebus_timeout_service");
                             s.SetServiceName("rebus_timeout_service");

                             s.Service<TimeoutService>(c =>
                                                           {
                                                               c.ConstructUsing(() => new TimeoutService());
                                                               c.WhenStarted(t => t.Start());
                                                               c.WhenStopped(t => t.Stop());
                                                               c.WhenPaused(t => t.Pause());
                                                               c.WhenContinued(t => t.Continue());
                                                           });
                         });
        }
    }

    public class TimeoutService
    {
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Start()
        {
            Log.Info("Starting....");
        }

        public void Stop()
        {
            Log.Info("Stopping...");
        }

        public void Pause()
        {
            Log.Info("Paused");
        }

        public void Continue()
        {
            Log.Info("Continued");
        }
    }
}
