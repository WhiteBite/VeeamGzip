using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest
{
    class ParallelByteArrayTransformer
    {
        public delegate void TransformMethod(DataBlock dataBlock);
        public delegate void ConsumeMethod(DataBlock dataBlock);

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

        public bool Transform(BlockSupplier blockSupplier, TransformMethod transformMethod, ConsumeMethod consumeMethod)
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
                processors[i].Start(transformMethod);
                Thread.Sleep(20);
            }

            var consumer = new Thread(Consume) { Name = "Writer", IsBackground = true, Priority = ThreadPriority.Normal };
            threadList.Add(consumer);
            consumer.Start(consumeMethod);

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

        private void Process(object o)
        {
            TransformMethod transform = (TransformMethod)o;

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

                    transform(dataBlock);

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
            ConsumeMethod consume = (ConsumeMethod)o;
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
                        consume(dataBlock);
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
                        consume(dataBlock);
                    }
                    if (exit) break;
                }
            }
            catch (Exception e){
                _exception = e;
                _aborting = true;
            }
        }
    }
}
