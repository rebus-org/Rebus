   using System;
   using System.Data.SqlClient;
   using System.Linq;
   using System.Net;

namespace IpUtil
{
    public class Lookup
    {
        public static bool IsLocalIpAddress(Uri uri)
        {
            return IsLocalIpAddress(uri.Host);
        }
        public static bool IsLocalIpAddress(SqlConnectionStringBuilder connectionStringBuilder)
        {
            return IsLocalIpAddress(connectionStringBuilder.DataSource);
        }

        public static bool IsLocalIpAddress(string host)
        {
            if (String.IsNullOrEmpty(host))
            {
                return false;
            }
            try
            {
                // get host IP addresses
                var hostIPs = Dns.GetHostAddresses(host);
                // get local IP addresses
                var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                return hostIPs.Any(hostIp => IPAddress.IsLoopback(hostIp) || localIPs.Contains(hostIp));
            }
            catch
            {
            }
            return false;
        }
    }
}