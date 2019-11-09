using System.Collections.Generic;
using System.Threading;

namespace ZipperVeeam
{
    class MyConcurrentQueue<T>
    {
        private readonly Queue<T> _store = new Queue<T>();

        private readonly Semaphore _semEnqueue;
        private readonly Semaphore _semDequeue;

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
                return false;
            }
        }
    }
}