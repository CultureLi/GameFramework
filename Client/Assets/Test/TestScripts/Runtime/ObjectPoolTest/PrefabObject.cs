using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Test.Runtime.ObjectPoolTest
{
    /*internal class PrefabObject : ObjectBase
    {
        public static PrefabObject Create(object target)
        {
            var obj = ReferencePool.Acquire<PrefabObject>();
            obj.Initialize("PrefabObject", target);
            return obj;
        }
        protected override void Release(bool isShutdown)
        {
            UnityEngine.Object.Destroy(Target as GameObject);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            var go = (Target as GameObject);
            go.transform.position = new Vector3(UnityEngine.Random.Range(-10, 10), UnityEngine.Random.Range(-5, 5), 0);
            go.SetActive(true);
        }

        protected override void OnUnspawn()
        {
            base.OnUnspawn();
            (Target as GameObject).SetActive(false);
        }
    }*/
}
