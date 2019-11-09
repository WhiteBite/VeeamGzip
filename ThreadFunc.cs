using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ZipperVeeam
{
    class ThreadFunc
    {
        private readonly MyConcurrentQueue<DataBlock> _queue1 = new MyConcurrentQueue<DataBlock>(Constants.QueueSize);
        private readonly MyConcurrentQueue<DataBlock> _queue2 = new MyConcurrentQueue<DataBlock>(Constants.QueueSize);
        private Exception _ex;
        private readonly object _locker = new object();

        bool _exiting = false;
        public Exception localException
        {
            get
            {
                lock (_locker)
                    return _ex;
            }
            set
            {
                lock (_locker)
                    _ex = value;
            }
        }

        /// <summary>
        /// false - Decompress;
        /// true - Compress
        /// </summary>
        /// <param name="compressMethod"></param>
        public ThreadFunc()
        {
            _ex = null;
        }


        public virtual void Work(DataBlock dataBlock)
        {

        }
        public void Supply(object o)
        {
            try
            {
                BlockSupplier supp = (BlockSupplier)o;
                DataBlock dataBlock; do
                {
                    dataBlock = supp.Next();

                    while (!_queue1.TryEnqueue(dataBlock) && localException != null) ;
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
            try
            {
                while (true)
                {
                    DataBlock dataBlock;
                    while (!_queue1.TryDequeue(out dataBlock) && localException == null && !_exiting) ;
                    if (localException != null || _exiting) break;

                    if (dataBlock.Size == 0)
                    {
                        while (!_queue2.TryEnqueue(dataBlock) && localException != null) ;
                        _exiting = true;
                        break;
                    }
                    Work(dataBlock);
                    while (!_queue2.TryEnqueue(dataBlock) && localException != null) ;
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
                List<DataBlock> lostAndFound = new List<DataBlock>();
                int partNo = 0;
                bool exit = false;

                while (true)
                {
                    DataBlock dataBlock;
                    while (!_queue2.TryDequeue(out dataBlock, timeout: 100) && localException == null && !_exiting) ;
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
