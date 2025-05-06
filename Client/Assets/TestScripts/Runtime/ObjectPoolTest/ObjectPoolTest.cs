using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.TestScripts.Runtime.ObjectPoolTest
{
    public class ObjectPoolTest : MonoBehaviour
    {
        public GameObject template;
        public IObjectPoolManager poolManager;
        internal IObjectPool<CustomObject> myPool;

        Queue<GameObject> nowAliveObject = new Queue<GameObject>();
        private void Awake()
        {
            poolManager = Framework.FrameworkMgr.GetModule<IObjectPoolManager>();
            myPool = poolManager.CreateSingleSpawnObjectPool<CustomObject>("Custom",10, 5,1);

        }

        private void Update()
        {
            Framework.FrameworkMgr.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        public GameObject CreateObj()
        {
            var obj = myPool.Spawn("CustomObj");
            if (obj != null)
            {
                return obj.Target as GameObject;
            }

            var go = GameObject.Instantiate(template);
            obj = CustomObject.Create(go);
            myPool.Register(obj,true);

            return go;
        }

        public void DestroyObj()
        {
            if (nowAliveObject.Count > 0)
            {
                var go = nowAliveObject.Dequeue();
                myPool.Unspawn(go);
            }
            
        }


        public void Spawn()
        {
            var go = CreateObj();
            nowAliveObject.Enqueue(go);
            go.transform.SetParent(transform);
        }

        public void UnSpawn()
        {
            if (nowAliveObject.Count > 0)
            {
                var go = nowAliveObject.Dequeue();
                myPool.Unspawn(go);
            }
        }

        public void Count() 
        {
            Debug.Log($"Active: {nowAliveObject.Count}  pool: {myPool.Count}");
        }


    }
}
