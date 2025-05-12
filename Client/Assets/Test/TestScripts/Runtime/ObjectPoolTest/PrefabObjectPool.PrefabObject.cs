using Framework;
using System;
using System.Collections.Generic;

using UnityEngine;

namespace Test.Runtime.ObjectPoolTest
{
    public partial class PrefabObjectPool
    {
        private class PrefabObject : ObjectBase
        {
            public static PrefabObject Create(GameObject target)
            {
                var obj = ReferencePool.Acquire<PrefabObject>();
                obj.Initialize("PrefabObject", target);
                return obj;
            }
            protected override void Release(bool isShutdown)
            {
                UnityEngine.Object.Destroy(Target as GameObject);
            }
        }
    }
}
