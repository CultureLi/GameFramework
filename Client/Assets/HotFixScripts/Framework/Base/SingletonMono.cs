using UnityEngine;

namespace Framework
{
    public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static object locker = new();

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        instance = FindObjectOfType<T>();
                        if (instance == null)
                        {
                            GameObject go = new GameObject(typeof(T).Name + " (Singleton)");
                            instance = go.AddComponent<T>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}