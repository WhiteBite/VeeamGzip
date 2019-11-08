using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace ZipperVeeam
{
    class Compresser : ThreadFunc
    {
        public override void Work(DataBlock dataBlock)
        {
            using (var ms = new MemoryStream())
            {
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
}
