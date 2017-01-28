using System.IO;
using System.IO.Compression;

namespace Rebus.Compression
{
    /// <summary>
    /// Zipper that holds the zipping logic
    /// </summary>
    public class Zipper
    {
        /// <summary>
        /// Zips the byte array
        /// </summary>
        public byte[] Zip(byte[] uncompressedBytes)
        {
            using (var targetStream = new MemoryStream())
            {
                using (var sourceStream = new MemoryStream(uncompressedBytes))
                using (var zipStream = new GZipStream(targetStream, CompressionLevel.Optimal))
                {
                    sourceStream.CopyTo(zipStream);
                }
                return targetStream.ToArray();
            }
        }

        /// <summary>
        /// Unzips the byte array
        /// </summary>
        public byte[] Unzip(byte[] compressedBytes)
        {
            using (var targetStream = new MemoryStream())
            {
                using (var sourceStream = new MemoryStream(compressedBytes))
                using (var zipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                {
                    zipStream.CopyTo(targetStream);
                }
                return targetStream.ToArray();
            }
        }
    }
}