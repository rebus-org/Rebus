using System;
using System.IO;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;
using Rebus.Config;

namespace Rebus.DataBus
{
    public static class DataBusOptionsExtensions
    {
        public static StandardConfigurer<IDataBusStorage> EnableDataBus(this OptionsConfigurer configurer)
        {
            var standardConfigurer = StandardConfigurer<IDataBusStorage>.GetConfigurerFrom(configurer);

            return standardConfigurer;
        }
    }

    public static class DataBusAdvancedBusExtensions
    {
        public static IDataBus DataBus(this IAdvancedApi advancedApi)
        {
            return new DefaultDataBus();
        }

        class DefaultDataBus : IDataBus
        {
            readonly IDataBusStorage _dataBusStorage;

            public DefaultDataBus(IDataBusStorage dataBusStorage)
            {
                _dataBusStorage = dataBusStorage;
            }

            public async Task<DataBusAttachment> CreateAttachment(Stream source)
            {
                var id = Guid.NewGuid().ToString();

                await _dataBusStorage.Save(id, source);

                return new DataBusAttachment(id);
            }
        }
    }

    public interface IDataBus
    {
        Task<DataBusAttachment> CreateAttachment(Stream source);
    }
}