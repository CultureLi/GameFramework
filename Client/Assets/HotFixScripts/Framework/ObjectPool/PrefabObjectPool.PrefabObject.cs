using UnityEngine;

namespace Framework
{
    public partial class PrefabObjectPool
    {
        public class PrefabObjectBehaviour : MonoBehaviour
        {

        }

        private sealed class PrefabObject : ObjectBase
        {
            private Transform _root;
            public static PrefabObject Create(string name, GameObject target, Transform root)
            {
                var obj = ReferencePool.Acquire<PrefabObject>();
                obj.Initialize(name, target, root);
                target.AddComponent<PrefabObjectBehaviour>();
                return obj;
            }

            private void Initialize(string name, GameObject target, Transform root)
            {
                _root = root;
                Initialize(name, target);
            }

            protected internal override void Release(bool isShutdown)
            {
                Destroy(Target as GameObject);
            }

            protected internal override void OnUnspawn()
            {
                (Target as GameObject).transform.parent = _root;
            }
        }
    }
}
