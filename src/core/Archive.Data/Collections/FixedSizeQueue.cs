using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Archive.Data.Collections
{
    /// <summary>
    /// Provides blocking collection with size capabilities
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedSizedQueue<T> : IDisposable
    {
        private readonly ConcurrentQueue<T> _internalQueue = new ConcurrentQueue<T>();
        protected readonly object _lockObject = new object();
        protected bool _isDisposed;
        
        private readonly ManualResetEvent _fullCollectionResetEvent;
        
        public int Limit { get; }

        public FixedSizedQueue(int limit)
        {
            Limit = limit;
            
            _fullCollectionResetEvent = new ManualResetEvent(false);
        }

        /// <summary>
        /// Adds the item to the queue.
        /// </summary>
        /// <param name="item">The item to be added to the collection. The value can be a null reference.</param>
        /// <exception cref="System.InvalidOperationException">The collection has been marked as complete with regards to additions.</exception>
        /// <exception cref="System.ObjectDisposedException">The collection has been disposed.</exception>
        /// <exception cref="System.InvalidOperationException">The underlying collection didn't accept the item.</exception>
        /// <remarks>
        /// If a bounded capacity was specified when this instance of collection was initialized,
        /// a call to Add may block until space is available to store the provided item.
        /// </remarks>
        public virtual void Enqueue(T item)
        {
            CheckDisposed();

            while (true)
            {
                lock (_lockObject)
                {
                    // check that we achieve store limit
                    if (_internalQueue.Count == Limit)
                        _fullCollectionResetEvent.Reset();
                    else
                    {
                        EnqueueInternal(item);
                        break;
                    }
                }
                
                //wait next dequeue if needed
                _fullCollectionResetEvent.WaitOne();
            }
        }

        protected virtual void BeforeEnqueueInternal()
        {
        }

        protected void EnqueueInternal(T item)
        {
            BeforeEnqueueInternal();

            _internalQueue.Enqueue(item);
        }

        /// <summary>Takes an item from the collection.</summary>
        /// <returns>The item removed from the collection.</returns>
        /// <exception cref="System.OperationCanceledException">The collection is empty and has been marked as complete with regards to additions.</exception>
        /// <exception cref="System.ObjectDisposedException">The collection has been disposed.</exception>
        public virtual T Dequeue()
        {
            CheckDisposed();

            lock (_lockObject)
            {
                return DequeueInternal();
            }
        }

        protected virtual void BeforeDequeueInternal()
        {
            if (_internalQueue.Count == Limit)
            {
                _fullCollectionResetEvent.Set();
            }
        }

        protected T DequeueInternal()
        {
            BeforeDequeueInternal();

            if (TryDequeue(out var item))
                return item;

            return default(T);
        }

        /// <summary>
        /// Attempts to remove an item from the collection.
        /// </summary>
        /// <param name="item">The item removed from the collection.</param>
        /// <returns>true if an item could be removed; otherwise, false.</returns>
        /// <exception cref="System.ObjectDisposedException">The collection has been disposed.</exception>
        public bool TryDequeue(out T item)
        {
            CheckDisposed();

            return _internalQueue.TryDequeue(out item);
        }

        /// <summary>
        /// Gets a value indicating emptiness of the collection.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">The collection has been disposed.</exception>
        public bool IsEmpty
        {
            get
            {
                CheckDisposed();

                return _internalQueue.IsEmpty;
            }
        }

        /// <summary>
        /// Clears the collection.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">The collection has been disposed.</exception>
        public void Clear()
        {
            CheckDisposed();

            while (!_internalQueue.IsEmpty)
                _internalQueue.TryDequeue(out _);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _fullCollectionResetEvent?.Dispose();

                _isDisposed = true;
            }
        }

        /// <summary>Throws a System.ObjectDisposedException if the collection was disposed</summary>
        /// <exception cref="System.ObjectDisposedException">If the collection has been disposed.</exception>
        protected void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(FixedSizedQueue<T>));
            }
        }
    }
}