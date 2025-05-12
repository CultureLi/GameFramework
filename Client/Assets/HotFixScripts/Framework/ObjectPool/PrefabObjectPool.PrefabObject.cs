using UnityEngine;

namespace Framework
{
    public partial class PrefabObjectPool
    {
        private class PrefabObject : ObjectBase
        {
            public static PrefabObject Create(string name, GameObject target)
            {
                var obj = ReferencePool.Acquire<PrefabObject>();
                obj.Initialize(name, target);
                return obj;
            }
            protected internal override void Release(bool isShutdown)
            {
                UnityEngine.Object.Destroy(Target as GameObject);
            }
        }
    }
}
