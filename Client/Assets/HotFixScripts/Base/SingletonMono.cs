using UnityEngine;
namespace GameMain.Runtime.Base
{
    public class SingletonMono<T>: MonoBehaviour where T : SingletonMono<T>
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    Object.DontDestroyOnLoad(go);
                    instance = go.AddComponent<T>();

                }
                return instance;
            }
        }
    }
}

