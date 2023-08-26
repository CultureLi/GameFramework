using UnityEngine;
namespace GameEngine.Runtime.Base
{
    public class SingletonMonoBehaviour<T>: MonoBehaviour where T : SingletonMonoBehaviour<T>
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

