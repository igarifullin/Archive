using System;
using System.Threading;
using Archive.Data;
using Archive.Services.Compression;

namespace Archive.Services
{
    public class GZipService
    {
        public void Execute(CompressionParams @params, CancellationToken cancellationToken)
        {
            using (var processor = GetProcessor(@params))
            {
                processor.Execute(@params, cancellationToken);
            }
        }

        private BaseStreamProcessor GetProcessor(CompressionParams @params)
        {
            switch (@params.CommandType)
            {
                case CommandType.Compress:
                    return new Compressor();
                case CommandType.Decompress:
                    return new Decompressor();
                default:
                    throw new InvalidOperationException("Unknown command type");
            }
        }
    }
}