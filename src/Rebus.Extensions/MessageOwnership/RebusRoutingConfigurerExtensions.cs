using System.Collections.Generic;
using Rebus.Configuration;

namespace Rebus.Extensions.MessageOwnership
{
    public static class RebusRoutingConfigurerExtensions
    {
        /// <summary>
        /// Configures Rebus to expect endpoint mappings from several sources by using specified message ownership determiners.
        /// </summary>
        public static void FromSeveralSources(this RebusRoutingConfigurer configurer, params IDetermineMessageOwnership[] messageOwnershipDeterminers)
        {
            configurer.Use(new DetermineMessageOwnershipFromOtherDeterminers(messageOwnershipDeterminers));
        }

        /// <summary>
        /// Configures Rebus to expect endpoint mappings from several sources by using specified message ownership determiners.
        /// </summary>
        public static void FromSeveralSources(this RebusRoutingConfigurer configurer, IEnumerable<IDetermineMessageOwnership> messageOwnershipDeterminers)
        {
            configurer.Use(new DetermineMessageOwnershipFromOtherDeterminers(messageOwnershipDeterminers));
        }
    }
}