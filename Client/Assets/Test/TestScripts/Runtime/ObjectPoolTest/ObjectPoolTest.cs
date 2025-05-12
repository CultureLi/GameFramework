using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Test.Runtime.ObjectPoolTest
{
    public class ObjectPoolTest : MonoBehaviour
    {
        PrefabObjectPool _pool;

        Queue<GameObject> nowAliveObject = new Queue<GameObject>();
        private void Awake()
        {
            _pool = new PrefabObjectPool("Custom", 10, 5);
        }

        public void Spawn()
        {
            var go = _pool.Spawn("Assets/BundleRes/Prefab/Sphere.prefab");

            go.transform.position = new Vector3(UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-5, 5), 0);

            nowAliveObject.Enqueue(go);
            go.transform.SetParent(transform);
        }

        public void UnSpawn()
        {
            if (nowAliveObject.Count > 0)
            {
                var go = nowAliveObject.Dequeue();
                go.SetActive(false);
                _pool.UnSpawn(go);
            }
        }

        public void Count() 
        {
            Debug.Log($"Active: {nowAliveObject.Count}  pool: {_pool.Count}");
        }


    }
}
