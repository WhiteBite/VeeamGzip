using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using ZipperVeeam;

namespace GZipTest
{
    class ParallelByteArrayTransformer
    {
        bool _isCompress;

        private bool _aborting;
        private bool _exiting;

        private Exception _exception { set; get; }


        private int _processorCount;

        public void Cancel()
        {
            _aborting = true;
            _exception = new OperationCanceledException("Operation was cancelled by request from user.");
        }

        public ParallelByteArrayTransformer()
        {
            _processorCount = Environment.ProcessorCount;
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

        public bool Transform(BlockSupplier blockSupplier, Stream destination)
        {
            var threadList = new List<Thread>();

            var supplier = new Thread(Supply) { Name = "Supplier", IsBackground = true, Priority = ThreadPriority.Normal };
            threadList.Add(supplier);
            supplier.Start(blockSupplier);

            var processors = new Thread[_processorCount];
            for (int i = 0; i < _processorCount; i++)
            {
                processors[i] = new Thread(Process)
                { Name = $"worker {i}", IsBackground = true, Priority = ThreadPriority.AboveNormal };
                threadList.Add(processors[i]);
                processors[i].Start();
                Thread.Sleep(20);
            }

            var consumer = new Thread(Consume) { Name = "Writer", IsBackground = true, Priority = ThreadPriority.Normal };
            threadList.Add(consumer);
            consumer.Start(destination);

            supplier.Join();
            for (int i = 0; i < processors.Length; i++)
            {
                processors[i].Join();
            }
            consumer.Join();

            return !_aborting;
        }

        ConcurrentQueue<DataBlock> queue1 = new ConcurrentQueue<DataBlock>(20);
        ConcurrentQueue<DataBlock> queue2 = new ConcurrentQueue<DataBlock>(20);

        private void Supply(object o)
        {
            try
            {
                BlockSupplier supp = (BlockSupplier)o;
                DataBlock dataBlock;
                do
                {
                    dataBlock = supp.Next();
                    while (!queue1.TryEnqueue(dataBlock, 100) && !_aborting) ;
                }
                while (dataBlock.Size > 0);
            }
            catch (Exception e)
            {
                _exception = e;
                _aborting = true;
            }

        }

        private void Process()
        {

            DataBlock dataBlock;

            try
            {
                while (true)
                {
                    while (!queue1.TryDequeue(out dataBlock, timeout: 100) && !_aborting && !_exiting) ;
                    if (_aborting || _exiting) break;

                    if (dataBlock.Size == 0)
                    {
                        while (!queue2.TryEnqueue(dataBlock, timeout: 100) && !_aborting) ;
                        _exiting = true;
                        break;
                    }

                    if (Constants.isCompress)
                        _CompressMethod(dataBlock);
                    else
                        _DeCompressMethod(dataBlock);
                    while (!queue2.TryEnqueue(dataBlock, timeout: 100) && !_aborting) ;
                }
            }
            catch (Exception e)
            {
                _exception = e;
                _aborting = true;
            }
        }

        private void Consume(object o)
        {
            Stream destination = (Stream)o;
           // ConsumeMethod consume = (ConsumeMethod)o;
            try
            {
                DataBlock dataBlock;
                List<DataBlock> lostAndFound = new List<DataBlock>();
                int partNo = 0;
                bool exit = false;

                while (true)
                {
                    while (!queue2.TryDequeue(out dataBlock, timeout: 100) && !_aborting) ;
                    if (_aborting) break;

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
                _exception = e;
                _aborting = true;
            }
        }
    }
}
