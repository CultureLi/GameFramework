namespace Framework
{
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static readonly object locker = new();

        public static T I
        {
            get
            {
                if (_instance == null)
                {
                    lock (locker)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                        }
                    }
                }
                return _instance;
            }
        }

        // 可选的释放方法
        public static void Dispose()
        {
            _instance = null;
        }
    }
}