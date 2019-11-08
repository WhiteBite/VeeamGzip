using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ZipperVeeam
{
    class ThreadFunc
    {
        private MyConcurrentQueue<DataBlock> queue1 = new MyConcurrentQueue<DataBlock>(Constants.QueueSize);
        private MyConcurrentQueue<DataBlock> queue2 = new MyConcurrentQueue<DataBlock>(Constants.QueueSize);
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
        /// false - Decompress;
        /// true - Compress
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
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fffff}]: [{Thread.CurrentThread.ManagedThreadId}] _ConsumeMethod block #{dataBlock.ID} start");
            destination.Write(dataBlock.Data, 0, dataBlock.Size);
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fffff")}]: [{Thread.CurrentThread.ManagedThreadId}] _ConsumeMethod block #{dataBlock.ID} end");
        }

        public void Supply(object o)
        { 
            try
            {
                BlockSupplier supp = (BlockSupplier)o;
                DataBlock dataBlock;
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fffff")}]: [{Thread.CurrentThread.ManagedThreadId}] Supply block start");
                do
                {
                    dataBlock = supp.Next();

                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fffff")}]: [{Thread.CurrentThread.ManagedThreadId}] Supply block #{dataBlock.ID}");
                    while (!queue1.TryEnqueue(dataBlock) && localException != null) ;
                }
                while (dataBlock.Size > 0);
            }
            catch (Exception e)
            {
                localException = e;
            }
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fffff")}]: [{Thread.CurrentThread.ManagedThreadId}] Supply block end");

        }

        public void Process()
        {
            DataBlock dataBlock;
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fffff")}]: [{Thread.CurrentThread.ManagedThreadId}] Processing block start");
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
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fffff")}]: [{Thread.CurrentThread.ManagedThreadId}] Processing block #{dataBlock.ID}");
                    if (isCompress)
                        _CompressMethod(dataBlock);
                    else
                        _DeCompressMethod(dataBlock);
                    while (!queue2.TryEnqueue(dataBlock) && localException != null) ;
                }
            }
            catch (Exception e)
            {
                localException = e;
            }
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fffff")}]: [{Thread.CurrentThread.ManagedThreadId}] Processing block end");
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
