using System;
using System.Collections.Generic;
using System.Threading;

namespace ZipperVeeam
{
    class MyConcurrentQueue<T>
    {
        private Queue<T> _store = new Queue<T>();

        private Semaphore _semEnqueue, _semDequeue;

        public MyConcurrentQueue(int capacity)
        {
            _semEnqueue = new Semaphore(capacity, capacity);
            _semDequeue = new Semaphore(0, capacity);
        }

        public bool TryEnqueue(T element, int timeout = Constants.Timeout)
        {
            if (_semEnqueue.WaitOne(timeout))
            {
                lock (_store)
                {
                    _store.Enqueue(element);
                    _semDequeue.Release();
                    return true;
                }
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fffff")}]: [{Thread.CurrentThread.ManagedThreadId}] TryEnqueue block end. Can`t get Semaphore");
                return false;
            }
        }

        public bool TryDequeue(out T element, int timeout = Constants.Timeout)
        {
            if (_semDequeue.WaitOne(timeout))
            {
                lock (_store)
                {
                    element = _store.Dequeue();
                    _semEnqueue.Release();
                    return true;
                }
            }
            else
            {
                element = default;
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fffff")}]: [{Thread.CurrentThread.ManagedThreadId}] TryDequeue block end. Can`t get Semaphore");
                return false;
            }
        }
    }
}