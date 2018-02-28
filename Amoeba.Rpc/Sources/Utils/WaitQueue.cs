using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Omnius.Base;

namespace Amoeba.Rpc
{
    class WaitQueue<T> : ManagerBase, ICollection<T>, IEnumerable<T>, ICollection, IEnumerable, ISynchronized
    {
        private Queue<T> _queue;
        private int? _capacity;
        private volatile ManualResetEvent _lowerResetEvent = new ManualResetEvent(false);
        private volatile ManualResetEvent _upperResetEvent = new ManualResetEvent(false);

        private readonly object _lockObject = new object();
        private volatile bool _isDisposed;

        public WaitQueue()
        {
            _queue = new Queue<T>();
        }

        public WaitQueue(int capacity)
        {
            _queue = new Queue<T>();
            _capacity = capacity;
        }

        public WaitQueue(IEnumerable<T> collection)
        {
            _queue = new Queue<T>();

            foreach (var item in collection)
            {
                this.Enqueue(item);
            }
        }

        public int Capacity
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                lock (this.LockObject)
                {
                    return _capacity ?? 0;
                }
            }
            set
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                lock (this.LockObject)
                {
                    _capacity = value;
                }
            }
        }

        public int Count
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                lock (this.LockObject)
                {
                    return _queue.Count;
                }
            }
        }

        public void Clear()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                _queue.Clear();
                if (_capacity != null) _upperResetEvent.Set();
            }
        }

        public bool Contains(T item)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                return _queue.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                _queue.CopyTo(array, arrayIndex);
            }
        }

        public T Dequeue()
        {
            return this.Dequeue(Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        public T Dequeue(TimeSpan timeout)
        {
            return this.Dequeue(timeout, CancellationToken.None);
        }

        public T Dequeue(CancellationToken token)
        {
            return this.Dequeue(Timeout.InfiniteTimeSpan, token);
        }

        public T Dequeue(TimeSpan timeout, CancellationToken token)
        {
            for (; ; )
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                lock (this.LockObject)
                {
                    if (_queue.Count > 0)
                    {
                        if (_capacity != null)
                        {
                            var item = _queue.Dequeue();

                            if (_queue.Count < _capacity.Value)
                            {
                                _upperResetEvent.Set();
                            }

                            return item;
                        }
                        else
                        {
                            return _queue.Dequeue();
                        }
                    }
                    else
                    {
                        _lowerResetEvent.Reset();
                    }
                }

                if (!_lowerResetEvent.WaitOne(timeout, token)) throw new TimeoutException();
            }
        }

        public void Enqueue(T item)
        {
            this.Enqueue(item, Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        public void Enqueue(T item, TimeSpan timeout)
        {
            this.Enqueue(item, timeout, CancellationToken.None);
        }

        public void Enqueue(T item, CancellationToken token)
        {
            this.Enqueue(item, Timeout.InfiniteTimeSpan, token);
        }

        public void Enqueue(T item, TimeSpan timeout, CancellationToken token)
        {
            for (; ; )
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                if (_capacity != null && _queue.Count >= _capacity.Value)
                {
                    if (!_upperResetEvent.WaitOne(timeout, token)) throw new TimeoutException();
                }

                lock (this.LockObject)
                {
                    if (_capacity != null)
                    {
                        if (_queue.Count < _capacity.Value)
                        {
                            _queue.Enqueue(item);
                            _lowerResetEvent.Set();

                            return;
                        }
                        else
                        {
                            _upperResetEvent.Reset();
                        }
                    }
                    else
                    {
                        _queue.Enqueue(item);
                        _lowerResetEvent.Set();

                        return;
                    }
                }
            }
        }

        public T Peek()
        {
            return this.Peek(Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        public T Peek(TimeSpan timeout)
        {
            return this.Peek(timeout, CancellationToken.None);
        }

        public T Peek(CancellationToken token)
        {
            return this.Peek(Timeout.InfiniteTimeSpan, token);
        }

        public T Peek(TimeSpan timeout, CancellationToken token)
        {
            for (; ; )
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                lock (this.LockObject)
                {
                    if (_queue.Count > 0)
                    {
                        return _queue.Peek();
                    }
                    else
                    {
                        _lowerResetEvent.Reset();
                    }
                }

                if (timeout < TimeSpan.Zero)
                {
                    if (!_lowerResetEvent.WaitOne(timeout, token)) throw new TimeoutException();
                }
            }
        }

        public T[] ToArray()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                return _queue.ToArray();
            }
        }

        public void TrimExcess()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                _queue.TrimExcess();
            }
        }

        public bool WaitDequeue()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            return _lowerResetEvent.WaitOne();
        }

        public bool WaitDequeue(TimeSpan timeout)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            return _lowerResetEvent.WaitOne(timeout);
        }

        public bool WaitEnqueue()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            return _upperResetEvent.WaitOne();
        }

        public bool WaitEnqueue(TimeSpan timeout)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            return _upperResetEvent.WaitOne(timeout);
        }

        public void Close()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                this.Dispose();
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                lock (this.LockObject)
                {
                    return false;
                }
            }
        }

        void ICollection<T>.Add(T item)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                this.Enqueue(item);
            }
        }

        bool ICollection<T>.Remove(T item)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                int count = _queue.Count;
                _queue = new Queue<T>(_queue.Where(n => !n.Equals(item)));
                if (_capacity != null) _upperResetEvent.Set();

                return (count != _queue.Count);
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                lock (this.LockObject)
                {
                    return true;
                }
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                return this.LockObject;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                ((ICollection)_queue).CopyTo(array, index);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                foreach (var item in _queue)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.LockObject)
            {
                return this.GetEnumerator();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (disposing)
            {
                if (_lowerResetEvent != null)
                {
                    try
                    {
                        _lowerResetEvent.Set();
                        _lowerResetEvent.Dispose();
                    }
                    catch (Exception)
                    {

                    }

                    _lowerResetEvent = null;
                }

                if (_upperResetEvent != null)
                {
                    try
                    {
                        _upperResetEvent.Set();
                        _upperResetEvent.Dispose();
                    }
                    catch (Exception)
                    {

                    }

                    _upperResetEvent = null;
                }
            }
        }

        #region IThisLock

        public object LockObject
        {
            get
            {
                return _lockObject;
            }
        }

        #endregion
    }
}
