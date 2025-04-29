using Framework;
using GameMain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.UI;
using UnityEngine;

namespace Assets.Scripts.ObjectPoolTest
{
    public class PoolTest : MonoBehaviour
    {
        public ButtonEditor button1;
        public GameObject targetGo;
        public IObjectPoolManager poolManager;
        internal IObjectPool<CustomObject> myPool;

        Queue<CustomObject> nowAliveObject=new Queue<CustomObject>();
        private void Awake()
        {
            poolManager = FrameworkEntry.GetModule<IObjectPoolManager>();
            myPool = poolManager.CreateSingleSpawnObjectPool<CustomObject>("Custom",10, 600);

            var obj = new CustomObject("CustomObj", targetGo);
            //for(int i = 0; i < 5;i++)
            myPool.Register(obj, false);
        }

        public void Spawn()
        {
            var go = myPool.Spawn("CustomObj");
            nowAliveObject.Enqueue(go);
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
