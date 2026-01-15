using System;
using UnityEngine;

namespace Framework
{
    public partial class PrefabObjectPool
    {
        public class PrefabObject : ObjectBase
        {
            private Transform _root;
            private Action<GameObject> _onPreRelease;
            public static PrefabObject Create(string name, GameObject target, Transform root, Action<GameObject> onPreRelease)
            {
                var obj = ReferencePool.Acquire<PrefabObject>();
                obj.Initialize(name, target, root);
                target.AddComponent<PrefabObjectBehaviour>();
                obj._onPreRelease = onPreRelease;
                return obj;
            }

            private void Initialize(string name, GameObject target, Transform root)
            {
                _root = root;
                Initialize(name, target);
            }
                
            protected internal override void Release()
            {
                _onPreRelease?.Invoke(Target as GameObject);
                GameObject.Destroy(Target as GameObject);
            }

            protected internal override void OnSpawn()
            {
                var go = (Target as GameObject);
                go.SetActive(true);
            }

            protected internal override void OnUnspawn()
            {
                var go = (Target as GameObject);
                go.transform.SetParent(_root);
                go.SetActive(false);
            }
        }
    }
}
