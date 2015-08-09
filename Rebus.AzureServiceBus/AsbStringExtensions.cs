using System.Linq;

namespace Rebus.AzureServiceBus
{
    /// <summary>
    /// Nifty extensions for making working with <see cref="AzureServiceBusTransport"/> easier
    /// </summary>
    public static class AsbStringExtensions
    {
        /// <summary>
        /// Gets a valid topic name from the given topic string. This conversion is a one-way destructive conversion!
        /// </summary>
        public static string ToValidAzureServiceBusEntityName(this string topic)
        {
            return string.Concat(topic
                .Select(c => char.IsLetterOrDigit(c) ? char.ToLower(c) : '_'));
        }
    }
}