using System;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Injection;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Rebus.DataBus
{
    /// <summary>
    /// Configuration extensions for Rebus' data bus
    /// </summary>
    public static class DataBusOptionsExtensions
    {
        /// <summary>
        /// Enables the data bus
        /// </summary>
        public static StandardConfigurer<IDataBusStorage> EnableDataBus(this OptionsConfigurer configurer)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer.Register<IDataBus>(c =>
            {
                var dataBusStorage = GetDataBusStorage(c);

                return new DefaultDataBus(dataBusStorage);
            });

            configurer.Decorate<IPipeline>(c =>
            {
                var dataBusStorage = GetDataBusStorage(c);
                var pipeline = c.Get<IPipeline>();

                var step = new DataBusIncomingStep(dataBusStorage);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(step, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep));
            });

            return StandardConfigurer<IDataBusStorage>.GetConfigurerFrom(configurer);
        }

        static IDataBusStorage GetDataBusStorage(IResolutionContext c)
        {
            try
            {
                return c.Get<IDataBusStorage>();
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, @"Could not get data bus storage - did you call 'EnableDataBus' without choosing a way to store the data?

When you enable the data bus, you must specify how to save data - it can be done by making further calls after 'EnableDataBus', e.g. like so:

Configure.With(..)
    .(...)
    .Options(o => o.EnableDataBus().StoreInSqlServer(....))
    .(...)");

            }
        }
    }
}