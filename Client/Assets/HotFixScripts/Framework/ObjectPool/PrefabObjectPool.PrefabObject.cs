using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    public partial class PrefabObjectPool
    {
        public class PrefabObject : ObjectBase
        {
            private Transform _root;
            public static PrefabObject Create(string location, GameObject target, Transform root)
            {
                var obj = ReferencePool.Acquire<PrefabObject>();
                obj.Initialize(location, target, root);
                return obj;
            }

            public static PrefabObject Create(string location, AsyncOperationHandle<GameObject> handle, Transform root)
            {
                return Create(location, handle.Result, root);
            }

            private void Initialize(string name, GameObject target, Transform root)
            {
                _root = root;
                Initialize(name, target);
            }

            protected internal override void Release()
            {
                var go = Target as GameObject;

                // 必须用 Unity 重载的 ==（对已销毁对象返回 true），不能用 ?. 或 ReferenceEquals：
                // 外部销毁触发的移除路径里 GameObject 已经是假 null，C# 包装引用还在，
                // 此时再调 GetComponent 会抛 MissingReferenceException。
                if (go == null)
                    return;

                var comp = go.GetComponent<AddressableInstanceHandle>();
                if (comp != null)
                    comp.ReleaseNow();

                // 有句柄时 ReleaseInstance 会顺带销毁对象；模板路径（Spawn(name, template)）
                // 没有句柄，ReleaseNow 不会销毁任何东西，必须在这里兜底，否则淘汰/Dispose 时对象会泄漏。
                // 对已排队销毁的对象重复 Destroy 是无害的 no-op。
                if (go != null)
                    GameObject.Destroy(go);
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
