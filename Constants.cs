using System;
using System.Collections.Generic;
using System.Text;

namespace ZipperVeeam
{
    public class Constants
    {
        public const int BufSize = 1024 * 1024;

        //  header zip archive http://www.onicos.com/staff/iz/formats/gzip.html 
        public const int HeaderSize = 10;
        public const byte HeaderByte1 = 0x1f;
        public const byte HeaderByte2 = 0x8b;
        public const byte CompressionMethod_Deflate = 0x08;
        public const int Timeout = 100;
        public const int QueueSize = 100;
        //TODO
        public static bool isCompress = true;
    }
}
