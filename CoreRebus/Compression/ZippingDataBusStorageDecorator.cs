using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Rebus.DataBus;
using Rebus.Extensions;

namespace Rebus.Compression
{
    /// <summary>
    /// Decorator for <see cref="IDataBusStorage"/> that GZIP-compresses data when it is streamed in/out
    /// </summary>
    public class ZippingDataBusStorageDecorator : IDataBusStorage
    {
        readonly IDataBusStorage _innerDataBusStorage;
        readonly DataCompressionMode _dataCompressionMode;

        /// <summary>
        /// Creates the decorator, wrapping the given <paramref name="innerDataBusStorage"/>
        /// </summary>
        public ZippingDataBusStorageDecorator(IDataBusStorage innerDataBusStorage, DataCompressionMode dataCompressionMode)
        {
            if (innerDataBusStorage == null) throw new ArgumentNullException(nameof(innerDataBusStorage));
            _innerDataBusStorage = innerDataBusStorage;
            _dataCompressionMode = dataCompressionMode;
        }

        /// <summary>
        /// Opens the data stored under the given ID for reading
        /// </summary>
        public async Task<Stream> Read(string id)
        {
            var metadata = await _innerDataBusStorage.ReadMetadata(id);

            string contentEncoding;

            if (!metadata.TryGetValue(MetadataKeys.ContentEncoding, out contentEncoding))
            {
                return await _innerDataBusStorage.Read(id);
            }

            if (!string.Equals(contentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
            {
                // unknown content encoding - the user must know best how to decode this!
                return await _innerDataBusStorage.Read(id);
            }

            var sourceStream = await _innerDataBusStorage.Read(id);

            return new GZipStream(sourceStream, CompressionMode.Decompress);
        }

        /// <summary>
        /// Loads the metadata stored with the given ID
        /// </summary>
        public async Task<Dictionary<string, string>> ReadMetadata(string id)
        {
            return await _innerDataBusStorage.ReadMetadata(id);
        }

        /// <summary>
        /// Saves the data from the given source stream under the given ID
        /// </summary>
        public async Task Save(string id, Stream source, Dictionary<string, string> metadata = null)
        {
            metadata = metadata?.Clone() ?? new Dictionary<string, string>();

            if (_dataCompressionMode == DataCompressionMode.Explicit)
            {
                string contentEncoding;

                if (!metadata.TryGetValue(MetadataKeys.ContentEncoding, out contentEncoding))
                {
                    await _innerDataBusStorage.Save(id, source, metadata);
                    return;
                }

                // who knows what the user did to the data? the user must know how to decode this data
                if (!string.Equals(contentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
                {
                    await _innerDataBusStorage.Save(id, source, metadata);
                    return;
                }
            }
            
            metadata[MetadataKeys.ContentEncoding] = "gzip";

            using (var destination = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(destination, CompressionLevel.Optimal, true))
                {
                    await source.CopyToAsync(gzipStream);
                }

                destination.Position = 0;

                await _innerDataBusStorage.Save(id, destination, metadata);
            }
        }
    }
}