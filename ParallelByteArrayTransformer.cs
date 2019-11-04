using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ZipperVeeam
{
    class ParallelByteArrayTransformer
    {
        private int _processorCount;


        public ParallelByteArrayTransformer()
        {
            _processorCount = Environment.ProcessorCount;
        }

        public bool Transform(BlockSupplier blockSupplier, Stream destination)
        {
            try
            {
                var threadList = new List<Thread>();
                ThreadFunc threadFunc = new ThreadFunc();
                var supplier = new Thread(threadFunc.Supply) { Name = "Supplier", IsBackground = true, Priority = ThreadPriority.Normal };
                threadList.Add(supplier);
                supplier.Start(blockSupplier);
                int procLim = _processorCount - 2;
                var processors = new Thread[procLim];
                for (int i = 0; i < procLim; i++)
                {
                    processors[i] = new Thread(threadFunc.Process)
                    { Name = $"worker {i}", IsBackground = true, Priority = ThreadPriority.AboveNormal };
                    threadList.Add(processors[i]);
                    processors[i].Start();
                }

                var consumer = new Thread(threadFunc.Consume) { Name = "Writer", IsBackground = true, Priority = ThreadPriority.Normal };
                threadList.Add(consumer);
                consumer.Start(destination);

                supplier.Join();
                for (int i = 0; i < processors.Length; i++)
                {
                    processors[i].Join();
                }
                consumer.Join();
            }
            catch (Exception e)
            {
                ZipperVeeam.HandlerGzip.PrintError(e);
                return false;
            }
            return true;
        }
    }
}
