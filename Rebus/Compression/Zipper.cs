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
                zipStream.Close();
                return targetStream.ToArray();
            }
        }

        /// <summary>
        /// Unzips the byte array
        /// </summary>
        public byte[] Unzip(byte[] compressedBytes)
        {
            using (var sourceStream = new MemoryStream(compressedBytes))
            using (var targetStream = new MemoryStream())
            using (var zipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
            {
                var buffer = new byte[1024];
                int nRead;
                while ((nRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    targetStream.Write(buffer, 0, nRead);
                }
                zipStream.Close();
                return targetStream.ToArray();
            }
        }

        /*
using (Stream fs = File.OpenRead("gj.txt"))
using (Stream fd = File.Create("gj.zip"))
using (Stream csStream = new GZipStream(fd, CompressionMode.Compress))
{
    byte[] buffer = new byte[1024];
    int nRead;
    while ((nRead = fs.Read(buffer, 0, buffer.Length))> 0)
    {
        csStream.Write(buffer, 0, nRead);
    }
}

using (Stream fd = File.Create("gj.new.txt"))
using (Stream fs = File.OpenRead("gj.zip"))
using (Stream csStream = new GZipStream(fs, CompressionMode.Decompress))
{
    byte[] buffer = new byte[1024];
    int nRead;
    while ((nRead = csStream.Read(buffer, 0, buffer.Length)) > 0)
    {
        fd.Write(buffer, 0, nRead);
    }
}
         */
    }
}