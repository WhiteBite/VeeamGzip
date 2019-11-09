using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZipperVeeam
{
    class Decompressor : ThreadFunc
    {
        public override void Work(DataBlock dataBlock)
        {
            using var ms = new MemoryStream(dataBlock.Data);
            using var gzip = new GZipStream(ms, CompressionMode.Decompress);
            var outbuf = new byte[ms.Length * 2];
            int bytesRead = 1, offset = 0;

            while (bytesRead > 0)
            {
                bytesRead = gzip.Read(outbuf, offset, outbuf.Length - offset);
                offset += bytesRead;

                if (offset == outbuf.Length) Array.Resize(ref outbuf, outbuf.Length * 2);
            }

            Array.Resize(ref outbuf, offset);
            dataBlock.Data = outbuf;
        }
    }
}
