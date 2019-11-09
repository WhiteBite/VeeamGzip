using System;
using System.IO;
using System.IO.Compression;

namespace ZipperVeeam
{
    internal class Compressor : ThreadFunc
    {
        public override void Work(DataBlock dataBlock)
        {
            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                gzip.Write(dataBlock.Data, 0, dataBlock.Size);
            }
            ms.Seek(4, 0);
            ms.Write(BitConverter.GetBytes((int)ms.Length), 0, 4);
            dataBlock.Data = ms.ToArray();
        }
    }
}
