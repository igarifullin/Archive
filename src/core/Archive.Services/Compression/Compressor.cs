using System;
using System.IO;
using System.IO.Compression;

namespace Archive.Services.Compression
{
    /// <summary>
    /// Provides stream processor for compression.
    /// </summary>
    public class Compressor : BaseStreamProcessor
    {
        protected override byte[] Process(byte[] block)
        {
            using (var memory = new MemoryStream())
            using (var gz = new GZipStream(memory, CompressionMode.Compress))
            {
                gz.Write(block, 0, block.Length);

                // WARNING: It is important to flush GZipStream, because it doesn't flush itself when Disposing
                // for more information read this - https://git.io/JeylB
                gz.Flush();

                return memory.ToArray();
            }
        }

        protected override void Write(Stream stream, byte[] block)
        {
            // write compressed block size in first 4 bytes, to show how many bytes we need read
            var compressedBlockLengthBytes = BitConverter.GetBytes(block.Length);
            stream.Write(compressedBlockLengthBytes, 0, compressedBlockLengthBytes.Length);

            stream.Write(block, 0, block.Length);
        }
    }
}