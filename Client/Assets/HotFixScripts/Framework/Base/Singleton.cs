public abstract class Singleton<T> where T : class, new()
{
    private static T instance;
    private static readonly object locker = new();

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new T();
                    }
                }
            }
            return instance;
        }
    }

    // 可选的释放方法
    public static void Dispose()
    {
        instance = null;
    }
}
