using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Rebus.Transports.Encrypted
{
    class GZipHelper
    {
        readonly int compressionThresholdBytes;

        public GZipHelper(int compressionThresholdBytes)
        {
            this.compressionThresholdBytes = compressionThresholdBytes;
        }

        /// <summary>
        /// Compresses the given byte array
        /// </summary>
        public Tuple<bool, byte[]> Compress(byte[] input)
        {
            if (input.Length <= compressionThresholdBytes)
                return Tuple.Create(false, input);

            using (var output = new MemoryStream())
            {
                using (var zip = new GZipStream(output, CompressionMode.Compress))
                {
                    zip.Write(input, 0, input.Length);
                }
                return Tuple.Create(true, output.ToArray());
            }
        }

        /// <summary>
        /// Decompresses the given byte array
        /// </summary>
        public byte[] Decompress(byte[] input)
        {
            using (var output = new MemoryStream(input))
            {
                using (var zip = new GZipStream(output, CompressionMode.Decompress))
                {
                    var bytes = new List<byte>();
                    var b = zip.ReadByte();
                    while (b != -1)
                    {
                        bytes.Add((byte)b);
                        b = zip.ReadByte();

                    }
                    return bytes.ToArray();
                }
            }
        }
    }
}