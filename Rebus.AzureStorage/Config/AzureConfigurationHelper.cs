using System;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;

namespace Rebus.AzureStorage.Config
{
    class AzureConfigurationHelper
    {
        public static CloudStorageAccount GetStorageAccount(string storageAccountConnectionStringOrName)
        {
            if (storageAccountConnectionStringOrName == null)
                throw new ArgumentNullException(nameof(storageAccountConnectionStringOrName));

            var isConnectionString = storageAccountConnectionStringOrName.ToLowerInvariant().Replace(" ", "").Contains("accountkey=");

            if (!isConnectionString)
            {
                var connectionStringSettings = ConfigurationManager.ConnectionStrings[storageAccountConnectionStringOrName];
                if (connectionStringSettings == null)
                {
                    throw new ConfigurationErrorsException($"Could not find connection string named '{storageAccountConnectionStringOrName}' in the current application configuration file");
                }
                storageAccountConnectionStringOrName = connectionStringSettings.ConnectionString;
            }

            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionStringOrName);
            return storageAccount;
        }
    }
}