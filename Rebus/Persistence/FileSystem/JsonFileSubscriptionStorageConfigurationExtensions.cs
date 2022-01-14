using Rebus.Config;
using Rebus.Subscriptions;

namespace Rebus.Persistence.FileSystem;

/// <summary>
/// Configurations extensions for configuring Rebus to use a JSON file as the subscription storage
/// </summary>
public static class JsonFileSubscriptionStorageConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to use a JSON file as the subscription storage
    /// </summary>
    public static void UseJsonFile(this StandardConfigurer<ISubscriptionStorage> configurer, string jsonFilePath)
    {
        configurer.Register(c => new JsonFileSubscriptionStorage(jsonFilePath));
    }
}