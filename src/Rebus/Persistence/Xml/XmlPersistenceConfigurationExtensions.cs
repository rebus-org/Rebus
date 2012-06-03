using System;
using System.IO;
using Rebus.Configuration.Configurers;

namespace Rebus.Persistence.Xml
{
    public static class XmlPersistenceConfigurationExtensions
    {
        /// <summary>
        /// Configures a <see cref="IStoreSubscriptions"/> that stores subscriptions in an XML file at
        /// the specified path.
        /// </summary>
        public static void StoreInXmlFile(this SubscriptionsConfigurer configurer, string filePath)
        {
            configurer.Use(new XmlSubscriptionStorage(filePath));
        }

        /// <summary>
        /// Configures a <see cref="IStoreSubscriptions"/> that stores subscriptions in an XML file in
        /// the base directory of the application. The default file name, "rebus-subscriptions.xml",
        /// will be used.
        /// </summary>
        public static void StoreInXmlFile(this SubscriptionsConfigurer configurer)
        {
            configurer.Use(new XmlSubscriptionStorage(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rebus-subscriptions.xml")));
        }
    }
}