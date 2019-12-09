using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Archive.Services.Compression
{
    /// <summary>
    /// Provides stream processor for decompression.
    /// </summary>
    public class Decompressor : BaseStreamProcessor
    {
        protected override byte[] Read(Stream stream)
        {
            var compressedBlockSizeBytes = new byte[4];
            stream.Read(compressedBlockSizeBytes, 0, 4);

            // Read block size
            var compressedBlockSize = BitConverter.ToInt32(compressedBlockSizeBytes, 0);
            if (compressedBlockSize < 0)
                throw new InvalidOperationException("Compressed file block corrupted");

            var compressedBlock = new byte[compressedBlockSize];
            var bytesRead = stream.Read(compressedBlock, 0, compressedBlockSize);

            return bytesRead > 0
                ? compressedBlock.Take(bytesRead).ToArray()
                : null;
        }

        protected override byte[] Process(byte[] block)
        {
            var length = _params.BlockSize;

            using (GZipStream gz = new GZipStream(new MemoryStream(block), CompressionMode.Decompress))
            {
                var decompressedBlock = new byte[length];
                var readBytes = gz.Read(decompressedBlock, 0, length);

                return readBytes == _params.BlockSize
                    ? decompressedBlock
                    : decompressedBlock.Take(readBytes).ToArray();
            }
        }
    }
}