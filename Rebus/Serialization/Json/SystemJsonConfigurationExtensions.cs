using System;
using System.Text;
using Rebus.Config;
using System.Text.Json;
// ReSharper disable UnusedMember.Global

namespace Rebus.Serialization.Json;

/// <summary>
/// Configuration extensions for the .NET System.Text.Json Rebus message serializer
/// </summary>
public static class SystemJsonConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to use .NET System.Text.Json to serialize messages.
    /// Default <see cref="JsonSerializerOptions" /> settings, except trailing commas are allowed, and comments are skipped.
    /// Message bodies are UTF8-encoded.
    /// This is the default message serialization, so there is actually no need to call this method.
    /// </summary>
    public static void UseSystemTextJson(this StandardConfigurer<ISerializer> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        RegisterSerializer(configurer);
    }

    /// <summary>
    /// Configures Rebus to use .NET System.Text.Json to serialize messages, using the specified <see cref="JsonSerializerOptions"/> and <see cref="Encoding"/>
    /// This allows you to customize almost every aspect of how messages are actually serialized/deserialized.
    /// </summary>
    public static void UseSystemTextJson(this StandardConfigurer<ISerializer> configurer, JsonSerializerOptions settings, Encoding encoding = null)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        RegisterSerializer(configurer, settings, encoding);
    }

    static void RegisterSerializer(StandardConfigurer<ISerializer> configurer, JsonSerializerOptions settings = null, Encoding encoding = null)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer.Register(c => new SystemTextJsonSerializer(c.Get<IMessageTypeNameConvention>(), settings, encoding));
    }
}