using System;
using Rebus.Config;
using Rebus.Messages;

namespace Rebus.Serialization.Custom;

/// <summary>
/// Extensions to help with configuring custom message type names
/// </summary>
public static class CustomTypeNameConventionExtensions
{
    /// <summary>
    /// Installs a message type name convention that can be customized by making further calls to the builder returned from this method.
    /// This can be used to improve interoperability of messages, as e.g.
    /// <code>
    /// Configure.With(...)
    ///     .(...)
    ///     .Serialization(s => {
    ///         s.UseCustomMessageTypeNames()
    ///             .AddWithCustomName&lt;SomeType&gt;("SomeType");
    ///     })
    ///     .Start();
    /// </code>
    /// This will make Rebus put the type name "SomeType" in the <see cref="Headers.Type"/> header, thus removing all of the .NET-specific
    /// stuff like namespace and assembly information.
    /// </summary>
    public static CustomTypeNameConventionBuilder UseCustomMessageTypeNames(this StandardConfigurer<ISerializer> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        var builder = new CustomTypeNameConventionBuilder();

        configurer
            .OtherService<IMessageTypeNameConvention>()
            .Register(_ => builder.GetConvention());

        return builder;
    }
}