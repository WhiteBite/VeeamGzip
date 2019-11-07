using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace ZipperVeeam
{
    class ThreadFunc
    {
        private ConcurrentQueue<DataBlock> queue1 = new ConcurrentQueue<DataBlock>(Constants.QueueSize);
        private ConcurrentQueue<DataBlock> queue2 = new ConcurrentQueue<DataBlock>(Constants.QueueSize);
        private Exception ex;
        private object locker = new object();

        bool isCompress = false;
        bool _exiting = false;
        public Exception localException
        {
            get
            {
                lock (locker)
                    return ex;
            }
            set
            {
                lock (locker)
                    ex = value;
            }
        }

        /// <summary>
        /// 0 - Decompress; 1 - Compress
        /// </summary>
        /// <param name="compressMethod"></param>
        public ThreadFunc(bool compressMethod)
        {
            ex = null;
            isCompress = compressMethod;
        }


        public void _CompressMethod(DataBlock dataBlock)
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

        public void _DeCompressMethod(DataBlock dataBlock)
        {
            using (var ms = new MemoryStream(dataBlock.Data))
            using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
            {
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

        public void _ConsumeMethod(DataBlock dataBlock, Stream destination)
        {
            destination.Write(dataBlock.Data, 0, dataBlock.Size);
        }

        public void Supply(object o)
        {
            try
            {
                BlockSupplier supp = (BlockSupplier)o;
                DataBlock dataBlock;
                do
                {
                    dataBlock = supp.Next();
                    while (!queue1.TryEnqueue(dataBlock) && localException != null) ;
                }
                while (dataBlock.Size > 0);
            }
            catch (Exception e)
            {
                localException = e;
            }

        }

        public void Process()
        {
            DataBlock dataBlock;
            try
            {
                while (true)
                {
                    while (!queue1.TryDequeue(out dataBlock) && localException == null && !_exiting) ;
                    if (localException != null || _exiting) break;

                    if (dataBlock.Size == 0)
                    {
                        while (!queue2.TryEnqueue(dataBlock) && localException != null) ;
                        _exiting = true;
                        break;
                    }

                    if (Environment.GetCommandLineArgs()[1].ToLower() == "compress")
                        _CompressMethod(dataBlock);
                    else if (Environment.GetCommandLineArgs()[1].ToLower() == "decompress")
                        _DeCompressMethod(dataBlock);
                    while (!queue2.TryEnqueue(dataBlock) && localException != null) ;
                }
            }
            catch (Exception e)
            {
                localException = e;
            }
        }

        public void Consume(object o)
        {
            Stream destination = (Stream)o;
            try
            {
                DataBlock dataBlock;
                List<DataBlock> lostAndFound = new List<DataBlock>();
                int partNo = 0;
                bool exit = false;

                while (true)
                {
                    while (!queue2.TryDequeue(out dataBlock, timeout: 100) && localException == null && !_exiting) ;
                    if (localException != null) break;

                    if (dataBlock.ID == partNo)
                    {
                        _ConsumeMethod(dataBlock, destination);
                        partNo++;
                    }
                    else lostAndFound.Add(dataBlock);

                    for (int i = 0; i < lostAndFound.Count;)
                    {
                        dataBlock = lostAndFound.Find(x => x.ID == partNo);

                        if (dataBlock == null) break;

                        partNo++;
                        lostAndFound.Remove(dataBlock);

                        if (dataBlock.Size == 0)
                        {
                            exit = true;
                            break;
                        }
                        _ConsumeMethod(dataBlock, destination);
                    }
                    if (exit) break;
                }
            }
            catch (Exception e)
            {
                localException = e;
            }
        }
    }
}
