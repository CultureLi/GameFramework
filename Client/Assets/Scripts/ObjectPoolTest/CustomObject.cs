using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.ObjectPoolTest
{
    internal class CustomObject : ObjectBase
    {
        public static CustomObject Create(object target)
        {
            var obj = ReferencePool.Acquire<CustomObject>();
            obj.Initialize("CustomObj",target);
            return obj;
        }
        protected override void Release(bool isShutdown)
        {
            UnityEngine.Object.Destroy(Target as GameObject);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            (Target as GameObject).SetActive(true);
        }

        protected override void OnUnspawn()
        {
            base.OnUnspawn();
            (Target as GameObject).SetActive(false);
        }
    }
}
