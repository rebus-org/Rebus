using System;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.Serialization;

namespace Rebus.Compression;

/// <summary>
/// Configuration extensions for enabling compression
/// </summary>
public static class ZipConfigurationExtensions
{
    /// <summary>
    /// Default threshold for the body size for compression to kick in
    /// </summary>
    public const int DefaultBodyThresholdBytes = 1024;

    /// <summary>
    /// Enables compression of outgoing messages if the size exceeds the specified number of bytes
    /// (defaults to <see cref="DefaultBodyThresholdBytes"/>)
    /// </summary>
    public static void EnableCompression(this OptionsConfigurer configurer, int bodySizeThresholdBytes = DefaultBodyThresholdBytes)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            
        configurer.Decorate<ISerializer>(c => new ZippingSerializerDecorator(c.Get<ISerializer>(), new Zipper(), bodySizeThresholdBytes));
    }

    /// <summary>
    /// Enables GZIP of the saved data bus data. Set <paramref name="dataCompressionMode"/> to control when data is gzipped - if <see cref="DataCompressionMode.Always"/>
    /// is selected the data will always be GZIPped, whereas selecting <see cref="DataCompressionMode.Explicit"/> makes the data be GZIPped
    /// only when <see cref="MetadataKeys.ContentEncoding"/> = "gzip" is detected among the metadata for the stored data.
    /// Please note that GZIPping the data requires that it can be fully contained in memory because the underlying streaming APIs do not support lazy-reading a
    /// GZIP stream.
    /// </summary>
    public static StandardConfigurer<IDataBusStorage> UseCompression(this StandardConfigurer<IDataBusStorage> configurer, DataCompressionMode dataCompressionMode)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer.Decorate(c =>
        {
            var dataBusStorage = c.Get<IDataBusStorage>();

            return new ZippingDataBusStorageDecorator(dataBusStorage, dataCompressionMode);
        });

        return configurer;
    }
}