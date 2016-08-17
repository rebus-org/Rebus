using System;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Transport;
using Rebus.Workers.ThreadBased;

namespace Rebus.Workers.ThreadPoolBased
{
    /// <summary>
    /// Configuration extensions for classic dedicated Rebus worker factory
    /// </summary>
    public static class DedicatedRebusWorkersConfigurationExtensions
    {
        /// <summary>
        /// Replaces the default thread pool-based worker factory with one that uses dedicate Rebus workers for everything (including running continuations)
        /// </summary>
        [Obsolete("This configuration method is going away when thread pool-based workers have been verified to work better in all scenarios")]
        public static void UseClassicRebusWorkersMessageDispatch(this OptionsConfigurer configurer)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            //configurer.Register<IWorkerFactory>(c =>
            //{
            //    var transport = c.Get<ITransport>();
            //    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            //    var pipeline = c.Get<IPipeline>();
            //    var pipelineInvoker = c.Get<IPipelineInvoker>();
            //    var options = c.Get<Options>();
            //    var busLifetimeEvents = c.Get<BusLifetimeEvents>();
            //    var backoffStrategy = c.Get<ISyncBackoffStrategy>();
            //    return new ThreadPoolWorkerFactory(transport, rebusLoggerFactory, pipeline, pipelineInvoker, options, c.Get<RebusBus>, busLifetimeEvents, backoffStrategy);
            //});

            configurer.Register<IWorkerFactory>(c =>
            {
                var transport = c.Get<ITransport>();
                var pipeline = c.Get<IPipeline>();
                var pipelineInvoker = c.Get<IPipelineInvoker>();
                var backoffStrategy = c.Get<IBackoffStrategy>();
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                var options = c.Get<Options>();
                return new ThreadWorkerFactory(transport, pipeline, pipelineInvoker, backoffStrategy, rebusLoggerFactory, options, c.Get<RebusBus>);
            });
        }
    }
}