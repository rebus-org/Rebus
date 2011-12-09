// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using Rebus.Configuration.Configurers;

namespace Rebus.Configuration
{
    public static class EndpointMappingsExtensions
    {
        /// <summary>
        /// Configures Rebus to pick up endpoint mappings in NServiceBus format from the current app.config/web.config.
        /// </summary>
        public static void FromNServiceBusConfiguration(this EndpointMappingsConfigurer configurer)
        {
            configurer.Use(new DetermineDestinationFromNServiceBusEndpointMappings(new StandardAppConfigLoader()));
        }

        /// <summary>
        /// Configures Rebus to expect endpoint mappings to be on Rebus form.
        /// </summary>
        public static void FromRebusMappingsSection(this EndpointMappingsConfigurer configurer)
        {
            configurer.Use(new DetermineDestinationFromConfigurationSection());
        }
    }
}