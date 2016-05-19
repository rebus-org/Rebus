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
            
        }
    }

    public interface IDataBus
    {
        Task<DataBusAttachment> CreateAttachment(Stream source);
    }
}