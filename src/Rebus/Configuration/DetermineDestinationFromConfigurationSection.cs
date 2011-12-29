using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Reflection;
using Rebus.Logging;
using System.Linq;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configures endpoint mappings from a <see cref="RebusMappingsSection"/> configuration section.
    /// </summary>
    public class DetermineDestinationFromConfigurationSection : IDetermineDestination
    {
        static readonly ILog Log = RebusLoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly ConcurrentDictionary<Type, string> endpointMappings = new ConcurrentDictionary<Type, string>();

        public DetermineDestinationFromConfigurationSection()
        {
            try
            {
                var section = ConfigurationManager.GetSection("Rebus");

                if (section == null || !(section is RebusMappingsSection))
                {
                    throw new ConfigurationErrorsException(@"Could not find configuration section named 'Rebus' (or else
the configuration section was not of the Rebus.Configuration.RebusMappingsSection type?)

Please make sure that the declaration at the top matches the XML element further down. And please note
that it is NOT possible to rename this section, even though the declaration makes it seem like it.");
                }

                PopulateMappings((RebusMappingsSection) section);
            }
            catch (ConfigurationErrorsException e)
            {
                throw new ConfigurationException(
                    @"
An error occurred when trying to parse out the configuration of the RebusMappingsSection:

{0}

-

For this way of configuring endpoint mappings to work, you need to supply a correct configuration
section declaration in the <configSections> element of your app.config/web.config - like so:

    <configSections>
        <section name=""Rebus"" type=""Rebus.Configuration.RebusMappingsSection, Rebus"" />
        <!-- other stuff in here as well -->
    </configSections>

-and then you need a <RebusMappings> element some place further down the app.config/web.config,
like so:

    <Rebus>
        <Endpoints>
            <add Messages=""Name.Of.Assembly"" Endpoint=""message_owner_1""/>
            <add Messages=""Namespace.ClassName, Name.Of.Another.Assembly"" Endpoint=""message_owner_2""/>
        </Endpoints>
    </Rebus>

This example shows how it's possible to map all types from an entire assembly to an endpoint. 

This is the preferred way of mapping message types, because it is a sign that you have structure your code
in a nice 1-message-assembly-to-1-endpoint kind of way, which requires the least amount of configuration
and maintenance on your part.

You CAN, however, map a single type at a time (which is shown with the second mapping above), and these
explicit mappings WILL OVERRIDE assembly mappings. So if you map an entire assembly to an endpoint,
and you map one of the types from that assembly to another endpoint explicitly, the explicit mapping
will be the one taking effect.

",
                    e);
            }
        }

        void PopulateMappings(RebusMappingsSection configurationSection)
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

                    Log.Info("Mapping assembly: {0}", assemblyName);

                    var assembly = LoadAssembly(assemblyName);

                    foreach (var type in assembly.GetTypes())
                    {
                        Map(type, element.Endpoint);
                    }
                }
                else
                {
                    var typeName = element.Messages;

                    Log.Info("Mapping type: {0}", typeName);

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
            Log.Info("    {0} -> {1}", messageType, endpoint);
            
            if (endpointMappings.ContainsKey(messageType))
            {
                Log.Warn("    ({0} -> {1} overridden by -> {2})", messageType, endpointMappings[messageType], endpoint);
            }
            
            endpointMappings[messageType] = endpoint;
        }

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
    }
}