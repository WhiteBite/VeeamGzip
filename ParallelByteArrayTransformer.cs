﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ZipperVeeam
{
    internal class ParallelByteArrayTransformer
    {
        private readonly int _processorCount;


        public ParallelByteArrayTransformer()
        {
            _processorCount = Environment.ProcessorCount;
        }
        /// <summary>
        /// Transform method
        /// false - Decompress;
        /// true - Compress;
        /// </summary>
        public bool Transform(BlockSupplier blockSupplier, Stream destination,bool transformMethod)
        {
            try
            {
                var threadList = new List<Thread>();
                ThreadFunc threadFunc;
                if (transformMethod)
                {
                     threadFunc = new Compressor();
                }
                else
                {
                     threadFunc = new Decompressor();
                }

                var supplier = new Thread(threadFunc.Supply) { Name = "Supplier", IsBackground = true, Priority = ThreadPriority.Normal };
                threadList.Add(supplier);
                supplier.Start(blockSupplier);
                int procLim = _processorCount ;
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
                foreach (var t in processors)
                {
                    t.Join();
                }
                consumer.Join();
            }
            catch (Exception e)
            {
                InfoPrinter.PrintError(e);
                return false;
            }
            return true;
        }
    }
}
