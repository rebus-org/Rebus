using System.IO;
using System.IO.Compression;

namespace Rebus.Compression
{
    /// <summary>
    /// Zipper that holds the zipping logic
    /// </summary>
    public class Zipper
    {
        public byte[] Zip(byte[] uncompressedBytes)
        {
            using (var targetStream = new MemoryStream())
            using (var zipStream = new GZipStream(targetStream, CompressionMode.Compress))
            {
                zipStream.Write(uncompressedBytes, 0, uncompressedBytes.Length);
                zipStream.Flush();

                return targetStream.GetBuffer();
            }
        }

        public byte[] Unzip(byte[] compressedBytes)
        {
            using (var targetStream = new MemoryStream())
            using (var sourceStream = new MemoryStream(compressedBytes))
            using (var zipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
            {
                byte[] buffer = new byte[1024];
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