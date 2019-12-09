namespace Archive.Data
{
    public class SafeValue<T>
    {
        private readonly object _lockObj;
        private T _data;

        public SafeValue(T data)
        {
            _lockObj = new object();
            _data = data;
        }

        public T Value
        {
            get
            {
                lock (_lockObj)
                    return _data;
            }
            set
            {
                lock (_lockObj)
                    _data = value;
            }
        }

        public void SetValue(T value)
        {
            Value = value;
        }

        public T GetValue() => Value;
    }
}