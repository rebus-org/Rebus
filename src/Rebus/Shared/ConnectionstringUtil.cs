using System.Configuration;

namespace Rebus.Shared
{
    internal static class ConnectionStringUtil
    {
        internal static string GetConnectionStringToUse(string connectionStringOrConnectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringOrConnectionStringName];
            var connectionStringToUse = connectionStringSettings != null
                                            ? connectionStringSettings.ConnectionString
                                            : connectionStringOrConnectionStringName;
            return connectionStringToUse;
        }
    }
}
