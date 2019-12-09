namespace Archive.Data.Collections
{
    /// <summary>
    /// Provides queue element with index.
    /// </summary>
    /// <typeparam name="T">Queue element type</typeparam>
    public class IndexedQueueElement<T>
    {
        public int Index { get; }

        public T Value { get; }

        public IndexedQueueElement(int index, T value)
        {
            Index = index;
            Value = value;
        }
    }
}