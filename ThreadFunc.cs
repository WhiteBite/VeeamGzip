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
        public ThreadFunc()
        {
            ex = null;
        }


        public virtual void Work(DataBlock dataBlock)
        {

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
                    Work(dataBlock);
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
                        //_ConsumeMethod(dataBlock, destination);
                        destination.Write(dataBlock.Data, 0, dataBlock.Size);
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
                        //_ConsumeMethod(dataBlock, destination);
                        destination.Write(dataBlock.Data, 0, dataBlock.Size);
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
