using System;

namespace Archive.Data.Collections
{
    /// <summary>
    /// Provides blocking and bounding capabilities for thread-safe collections with index order
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SafeIndexQueue<T> : FixedSizedQueue<IndexedQueueElement<T>>
    {
        private int _index;
        private bool _isAddingComplete;

        public SafeIndexQueue(int capacity) : base(capacity)
        {
            _isAddingComplete = false;
            _index = 0;
        }

        /// <summary>
        /// Adds the item to the queue.
        /// </summary>
        /// <param name="item">The item to be added to the collection. The value can be a null reference.</param>
        /// <exception cref="System.InvalidOperationException">The collection has been marked as complete with regards to additions.</exception>
        /// <exception cref="System.ObjectDisposedException">The collection has been disposed.</exception>
        /// <remarks>
        /// If a bounded capacity was specified when this instance of collection was initialized,
        /// a call to Add may block until fit index will provide.
        /// </remarks>
        public override void Enqueue(IndexedQueueElement<T> item)
        {
            CheckDisposed();

            if (IsAddingCompleted)
            {
                throw new InvalidOperationException("Collection was marked as complete with regards to additions");
            }

            while (true)
            {
                lock (_lockObject)
                {
                    if (_index != item.Index)
                        continue;

                    EnqueueInternal(item);
                    _index++;
                    break;
                }
            }
        }

        /// <summary>Gets whether this collection has been marked as complete for adding.</summary>
        /// <value>Whether this collection has been marked as complete for adding.</value>
        /// <exception cref="System.ObjectDisposedException">The collection has been disposed.</exception>
        public bool IsAddingCompleted
        {
            get
            {
                CheckDisposed();

                lock (_lockObject)
                    return _isAddingComplete;
            }
        }

        /// <summary>
        /// Marks the colection instances as not accepting any more additions.
        /// </summary>
        /// <remarks>
        /// After a collection has been marked as complete for adding, adding to the collection is not permitted
        /// and attempts to remove from the collection will not wait when the collection is empty.
        /// </remarks>
        /// <exception cref="System.ObjectDisposedException">The collection has been disposed.</exception>
        public void CompleteAdding()
        {
            CheckDisposed();

            lock (_lockObject)
            {
                _isAddingComplete = true;
            }
        }

        /// <summary>Gets whether this collection has been marked as complete for adding and is empty.</summary>
        /// <value>Whether this collection has been marked as complete for adding and is empty.</value>
        /// <exception cref="System.ObjectDisposedException">The collection has been disposed.</exception>
        public bool IsCompleted => IsAddingCompleted && IsEmpty;
    }
}