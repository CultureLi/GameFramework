using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;

namespace PreserveStrip
{
    public class AOTgenericRef
    {
        /// 先在link.xml中加入要保留的程序集或类
        /// 如果你主工程中没有引用过某个程序集的任何代码，即使在link.xml中保留，该程序集也会被完全裁剪。因此对于每个要保留的AOT程序集， 请确保在主工程代码中显式引用过它的某个类或函数
        public static void RefAssembly()
        {
            _ = typeof(UnityEngine.UI.Button);
            _ = typeof(UnityEngine.Canvas);
            _ = typeof(K4os.Compression.LZ4.LZ4Codec);
            _ = typeof(Addressables);
            _ = typeof(ResourceManager);
        }

        //基于补充元数据的泛型函数实例化技术虽然相当完美，但毕竟实例化的函数以解释方式执行，如果能提前在AOT中泛型实例化，可以大幅提升性能。 所以对于常用尤其是性能敏感的泛型类和函数，可以提前在AOT中实例化。
        public static void AOTgenericInstance()
        {
            _ = new Dictionary<int, int>();
            _ = new Dictionary<int, uint>();
            _ = new Dictionary<int, object>();
            _ = new Dictionary<uint, uint>();
            _ = new Dictionary<uint, object>();
            _ = new Dictionary<long, object>();
            _ = new Dictionary<ulong, object>();
            _ = new Dictionary<object, object>();
            _ = new Dictionary<string, object>();
            _ = new Dictionary<object, int>();
            _ = new Dictionary<object, uint>();

            _ = new SortedDictionary<int, object>();
            _ = new SortedDictionary<long, object>();

            _ = (object)(new ValueTuple<int, object>());
            _ = (object)(new ValueTuple<float, object>());
            _ = (object)(new ValueTuple<long, object>());
            _ = (object)(new ValueTuple<object, object>());

            _ = new LinkedList<object>();

            _ = new List<int>();
            _ = new List<uint>();
            _ = new List<float>();
            _ = new List<double>();
            _ = new List<object>();
            _ = new List<long>();
            _ = new List<ulong>();
            _ = new List<string>();

            _ = new Queue<object>();
            _ = new Queue<int>();
            _ = new Queue<uint>();
            _ = new Queue<long>();

            _ = new HashSet<int>();
            _ = new HashSet<long>();
            _ = new HashSet<object>();

            _ = new SortedSet<int>();
            _ = new SortedSet<long>();

            _ = new Stack<object>();

            _ = new Action<int>(_ => { });
            _ = new Action<object>(_ => { });
            _ = new Action<byte>(_ => { });
            _ = new Action<bool>(_ => { });
            _ = new Action<float>(_ => { });
            _ = new Action<long>(_ => { });

            _ = new Func<int>(() => { return 0; });
            _ = new Func<int, int>((_) => { return 0; });
            _ = new Func<int, object>((_) => { return 0; });
            _ = new Func<float>(() => { return 0; });
            _ = new Func<long>(() => { return 0; });
            _ = new Func<object>(() => { return null; });
            _ = new Func<object, object>((_) => { return null; });
        }

        public static void AotGenerateMethods()
        {
            Addressables.LoadAssetAsync<object>(null);
            Addressables.Release<object>(obj:null);
            Addressables.Release(handle:default);
            Addressables.ReleaseInstance(instance:null);
            Addressables.ReleaseInstance(handle: default);

            Addressables.InstantiateAsync(location:null, parent:null);
            Addressables.InstantiateAsync(location:null, position:Vector3.zero, rotation:Quaternion.identity);
            Addressables.InstantiateAsync(key:null, position:Vector3.zero, rotation:Quaternion.identity);
            Addressables.InstantiateAsync(key:null, parent:null);
            Addressables.InstantiateAsync(key:null, instantiateParameters: default);

            K4os.Compression.LZ4.LZ4Codec.Decode(null, 0, 0, null, 0, 0);
            K4os.Compression.LZ4.LZ4Codec.Encode(null, 0, 0, null, 0, 0);

            var Go = new GameObject();
            _ = Go.transform.childCount;
            Go.transform.Find(string.Empty);
            Go.transform.GetChild(0);
            Go.AddComponent<Component>();
            Go.AddComponent(typeof(Component));
            Go.GetComponent<Component>();
            Go.GetComponent(typeof(Component));
            Go.GetComponentInChildren<Component>();
            Go.GetComponentInChildren(typeof(Component));
            Go.GetComponentsInChildren<Component>();
            Go.GetComponentsInChildren(typeof(Component));
            Go.GetComponentInParent<Component>();
            Go.GetComponentInParent(typeof(Component));
            Go.GetComponentsInParent<Component>();
            Go.GetComponentsInParent(typeof(Component));

            UnityEngine.Object.Instantiate(null);
            UnityEngine.Object.Instantiate(null, null);
            UnityEngine.Object.Instantiate(null, null, true);
            UnityEngine.Object.Instantiate(null, Vector3.one, Quaternion.identity);

        }

    }
}
