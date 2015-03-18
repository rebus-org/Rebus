using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure;

namespace Rebus.Tests.Contracts.Transports.Factories
{
    public static class AzureUtil
    {
        static readonly string AzureConnectionInfoFilePath =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                ".."
                , "..",
                "azure_connection_string.txt");

        static IEnumerable<string> GetLines()
        {
            if (!File.Exists(AzureConnectionInfoFilePath))
            {
                throw new ArgumentException(
                    string.Format(@"Could not find text file with Azure connection strings - looked here: {0}

If you want to run the Azure tests, please provide a text file containing a valid connection string, e.g. something like

Endpoint=sb://someServiceBusSomewhere.servicebus.windows.net/;SharedAccessKeyName=SomeAccessKeyThatCanAccesTopic;SharedAccessKey=baef57deadbputthekeyinhereb5eb8dfdef8ad

",
                        AzureConnectionInfoFilePath));
            }

            var allLines = File.ReadAllLines(AzureConnectionInfoFilePath);

            return allLines;
        }

        public static string AzureServiceBusConnectionString
        {
            get
            {
                var firstLine = GetLines().FirstOrDefault();

                if (firstLine == null)
                {
                    throw new ConfigurationErrorsException(string.Format("Found {0} but it did not contain any lines (expected the first line to be an Azure Service Bus connection string", AzureConnectionInfoFilePath));
                }

                return firstLine;
            }
        }

        public static CloudStorageAccount CloudStorageAccount
        {
            get
            {
                var storageAccountName = GetLines().Skip(1).FirstOrDefault();
                var storageAccountKey = GetLines().Skip(2).FirstOrDefault();

                if (storageAccountName == null || storageAccountKey == null)
                {
                    throw new ConfigurationErrorsException(string.Format("Found {0} but it did not contain enough lines (expected 2nd and 3rd line to be storage account name and key respectively)", AzureConnectionInfoFilePath));
                }

                var account = new CloudStorageAccount(new StorageCredentialsAccountAndKey(storageAccountName, storageAccountKey), true);
                return account;
            }
        }
    }
}