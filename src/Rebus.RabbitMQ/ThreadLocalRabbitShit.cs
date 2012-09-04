using System;
using RabbitMQ.Client;
using RabbitMQ.Client.MessagePatterns;
using Rebus.Logging;

namespace Rebus.RabbitMQ
{
    class ThreadLocalRabbitShit : IDisposable
    {
        static ILog log;

        static ThreadLocalRabbitShit()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        bool disposed;

        public IModel Model { get; set; }
        public Subscription Subscription { get; set; }
        public AmbientTxHack AmbientTxHack { get; set; }

        public void Dispose()
        {
            disposed = true;
            if (Subscription != null)
            {
                log.Debug("Disposing subscription");
                Subscription.Close();
            }

            if (Model != null)
            {
                log.Debug("Disposing model");
                Model.Close();
                Model.Dispose();
            }
        }

        public bool Disposed
        {
            get { return disposed; }
        }
    }
}