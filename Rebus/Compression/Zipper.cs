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
            using (var sourceStream = new MemoryStream(uncompressedBytes))
            using (var targetStream = new MemoryStream())
            using (var zipStream = new GZipStream(targetStream, CompressionLevel.Optimal))
            {
                var buffer = new byte[1024];
                int nRead;
                while ((nRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    zipStream.Write(buffer, 0, nRead);
                }
                zipStream.Flush();
                return targetStream.GetBuffer();
            }
        }

        /// <summary>
        /// Unzips the byte array
        /// </summary>
        public byte[] Unzip(byte[] compressedBytes)
        {
            using (var targetStream = new MemoryStream())
            using (var sourceStream = new MemoryStream(compressedBytes))
            using (var zipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
            {
                var buffer = new byte[1024];
                int nRead;
                while ((nRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    targetStream.Write(buffer, 0, nRead);
                }
                return targetStream.GetBuffer();
            }
        }
    }
}