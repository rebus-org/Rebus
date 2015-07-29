using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Reflection;
using Rebus.Logging;
using System.Linq;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configures endpoint mappings from a <see cref="RebusConfigurationSection"/> configuration section.
    /// </summary>
    public class DetermineMessageOwnershipFromRebusConfigurationSection : IDetermineMessageOwnership
    {
        static ILog log;
        readonly Func<Type, bool> typeFilter;

        static DetermineMessageOwnershipFromRebusConfigurationSection()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<Type, string> endpointMappings = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// Constructs the endpoint mapper, using the specified type filter to determine whether an encountered
        /// type should be mapped. Can be used to avoid mapping e.g. factories and stuff if you want to put
        /// helper classes inside your message assembly
        /// </summary>
        public DetermineMessageOwnershipFromRebusConfigurationSection(Func<Type, bool> typeFilter)
        {
            this.typeFilter = typeFilter;

            try
            {
                var section = RebusConfigurationSection.LookItUp();

                PopulateMappings(section);
            }
            catch (ConfigurationErrorsException e)
            {
                throw new ConfigurationException(
                    @"
An error occurred when trying to parse out the configuration of the RebusConfigurationSection:

{0}

-

For this way of configuring endpoint mappings to work, you need to supply a correct configuration
section declaration in the <configSections> element of your app.config/web.config - like so:

    <configSections>
        <section name=""rebus"" type=""Rebus.Configuration.RebusConfigurationSection, Rebus"" />
        <!-- other stuff in here as well -->
    </configSections>

-and then you need a <rebus> element some place further down the app.config/web.config,
like so:

{1}

This example shows how it's possible to map all types from an entire assembly to an endpoint. 

This is the preferred way of mapping message types, because it is a sign that you have structured your code
in a nice 1-message-assembly-to-1-endpoint kind of way, which requires the least amount of configuration
and maintenance on your part.

You CAN, however, map a single type at a time, and these explicit mappings WILL OVERRIDE assembly
mappings. So if you map an entire assembly to an endpoint, and you map one of the types from that
assembly to another endpoint explicitly, the explicit mapping will be the one taking effect.

Note also, that specifying the input queue name with the 'inputQueue' attribute is optional.
",
                    e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
            }
        }

        /// <summary>
        /// Constructs the endpoint mapper without a type filter
        /// </summary>
        public DetermineMessageOwnershipFromRebusConfigurationSection()
            : this(t => true)
        {
        }

        /// <summary>
        /// Gets the name of the endpoint that is configured to be the owner of the specified message type.
        /// </summary>
        public string GetEndpointFor(Type messageType)
        {
            string endpoint;
            if (endpointMappings.TryGetValue(messageType, out endpoint))
            {
                return endpoint;
            }

            var message = string.Format(@"Could not find an endpoint mapping for the message type {0}. 

Please ensure that you have mapped all message types, you wish to either Send or
Subscribe to, to an endpoint - a 'message owner' if you will.", messageType);

            throw new InvalidOperationException(message);
        }

        void PopulateMappings(RebusConfigurationSection configurationSection)
        {
            //ensure that all assembly mappings are processed first,
            //so that explicit type mappings will take precendence
            var mappingElements = configurationSection.MappingsCollection
                .OrderBy(c => !c.IsAssemblyName);

            foreach (var element in mappingElements)
            {
                if (element.IsAssemblyName)
                {
                    var assemblyName = element.Messages;

                    log.Info("Mapping assembly: {0}", assemblyName);

                    var assembly = LoadAssembly(assemblyName);

                    foreach (var type in assembly.GetTypes())
                    {
                        Map(type, element.Endpoint);
                    }
                }
                else
                {
                    var typeName = element.Messages;

                    log.Info("Mapping type: {0}", typeName);

                    var messageType = Type.GetType(typeName);

                    if (messageType == null)
                    {
                        throw new ConfigurationException(
                            @"Could not find the message type {0}. If you choose to map a specific message type,
please ensure that the type is available for Rebus to load. This requires that the
assembly can be found in Rebus' current runtime directory, that the type is available,
and that any (of the optional) version and key requirements are matched",
                            typeName);
                    }

                    Map(messageType, element.Endpoint);
                }
            }
        }

        static Assembly LoadAssembly(string assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (Exception e)
            {
                throw new ConfigurationException(
                    @"
Something went wrong when trying to load message types from assembly {0}

{1}

For this to work, Rebus needs access to an assembly with one of the following filenames:

    {0}.dll
    {0}.exe

",
                    assemblyName, e);
            }
        }

        void Map(Type messageType, string endpoint)
        {
            if (!typeFilter(messageType)) return;

            log.Info("    {0} -> {1}", messageType, endpoint);
            
            if (endpointMappings.ContainsKey(messageType))
            {
                log.Warn("    ({0} -> {1} overridden by -> {2})", messageType, endpointMappings[messageType], endpoint);
            }
            
            endpointMappings[messageType] = endpoint;
        }
    }
}