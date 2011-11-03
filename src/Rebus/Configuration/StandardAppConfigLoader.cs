using System;
using System.IO;

namespace Rebus.Configuration
{
    /// <summary>
    /// <see cref="IAppConfigLoader"/> that can load the application configuration 
    /// file of the currently activated AppDomain.
    /// </summary>
    public class StandardAppConfigLoader : IAppConfigLoader
    {
        public string LoadIt()
        {
            var pathToAppConfig = AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE") as string;

            return string.IsNullOrEmpty(pathToAppConfig)
                       ? ""
                       : File.ReadAllText(pathToAppConfig);
        }
    }
}