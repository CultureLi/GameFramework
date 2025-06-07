using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Google.Protobuf.dll",
		"System.Core.dll",
		"System.dll",
		"Unity.Addressables.dll",
		"Unity.ResourceManager.dll",
		"UnityEngine.AndroidJNIModule.dll",
		"UnityEngine.CoreModule.dll",
		"UnityEngine.JSONSerializeModule.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// DelegateList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>
	// DelegateList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// DelegateList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// DelegateList<float>
	// Google.Protobuf.Collections.RepeatedField.<GetEnumerator>d__29<object>
	// Google.Protobuf.Collections.RepeatedField<object>
	// Google.Protobuf.FieldCodec.<>c<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass38_0<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass39_0<object>
	// Google.Protobuf.FieldCodec.InputMerger<object>
	// Google.Protobuf.FieldCodec.ValuesMerger<object>
	// Google.Protobuf.FieldCodec<object>
	// Google.Protobuf.IDeepCloneable<object>
	// Google.Protobuf.IMessage<object>
	// Google.Protobuf.MessageParser.<>c__DisplayClass2_0<object>
	// Google.Protobuf.MessageParser<object>
	// Google.Protobuf.ValueReader<object>
	// Google.Protobuf.ValueWriter<object>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,object>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Action<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Action<byte>
	// System.Action<float>
	// System.Action<int>
	// System.Action<object,object>
	// System.Action<object>
	// System.ByReference<UnityEngine.jvalue>
	// System.ByReference<object>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<Framework.TypeNamePair,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,uint>
	// System.Collections.Generic.Dictionary.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<Framework.TypeNamePair,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,uint>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection<Framework.TypeNamePair,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,uint>
	// System.Collections.Generic.Dictionary.KeyCollection<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<Framework.TypeNamePair,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,uint>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection<Framework.TypeNamePair,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,uint>
	// System.Collections.Generic.Dictionary.ValueCollection<uint,object>
	// System.Collections.Generic.Dictionary<Framework.TypeNamePair,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<object,uint>
	// System.Collections.Generic.Dictionary<uint,object>
	// System.Collections.Generic.EqualityComparer<Framework.TypeNamePair>
	// System.Collections.Generic.EqualityComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.EqualityComparer<uint>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<Framework.TypeNamePair,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,uint>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.ICollection<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Framework.TypeNamePair,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,uint>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerable<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<Framework.TypeNamePair,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,uint>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<Framework.TypeNamePair>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IEqualityComparer<uint>
	// System.Collections.Generic.IList<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<Framework.TypeNamePair,object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.KeyValuePair<object,uint>
	// System.Collections.Generic.KeyValuePair<uint,object>
	// System.Collections.Generic.LinkedList.Enumerator<object>
	// System.Collections.Generic.LinkedList<object>
	// System.Collections.Generic.LinkedListNode<object>
	// System.Collections.Generic.List.Enumerator<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<Framework.TypeNamePair>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<uint>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Comparison<int>
	// System.Comparison<object>
	// System.Func<System.Threading.Tasks.VoidTaskResult>
	// System.Func<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>
	// System.Func<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Func<object,System.Threading.Tasks.VoidTaskResult>
	// System.Func<object,UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Func<object,byte>
	// System.Func<object,int>
	// System.Func<object,object>
	// System.Func<object>
	// System.IEquatable<Framework.TypeNamePair>
	// System.IEquatable<object>
	// System.Nullable<int>
	// System.Predicate<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>
	// System.Predicate<int>
	// System.Predicate<object>
	// System.ReadOnlySpan<UnityEngine.jvalue>
	// System.ReadOnlySpan<object>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<object>
	// System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.TaskAwaiter<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Span<UnityEngine.jvalue>
	// System.Span<object>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<object>
	// System.Threading.Tasks.Task<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.Task<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Threading.Tasks.Task<object>
	// System.Threading.Tasks.TaskCompletionSource<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Threading.Tasks.TaskCompletionSource<object>
	// System.Threading.Tasks.TaskFactory<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// System.Threading.Tasks.TaskFactory<object>
	// UnityEngine.AddressableAssets.AddressablesImpl.<>c__DisplayClass78_0<object>
	// UnityEngine.AddressableAssets.AddressablesImpl.<>c__DisplayClass79_0<object>
	// UnityEngine.AddressableAssets.AddressablesImpl.<>c__DisplayClass88_0<object>
	// UnityEngine.AddressableAssets.AddressablesImpl.<>c__DisplayClass89_0<object>
	// UnityEngine.AddressableAssets.AddressablesImpl.<>c__DisplayClass91_0<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.<>c__DisplayClass60_0<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.<>c__DisplayClass60_0<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.<>c__DisplayClass61_0<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase.<>c__DisplayClass61_0<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.<>c<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.<>c<object>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>
	// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>
	// UnityEngine.ResourceManagement.ChainOperationTypelessDepedency<object>
	// UnityEngine.ResourceManagement.ResourceManager.<>c__DisplayClass100_0<object>
	// UnityEngine.ResourceManagement.ResourceManager.CompletedOperation<object>
	// UnityEngine.ResourceManagement.Util.GlobalLinkedListNodeCache<object>
	// UnityEngine.ResourceManagement.Util.LinkedListNodeCache<object>
	// }}

	public void RefMethods()
	{
		// object System.Activator.CreateInstance<object>()
		// object[] System.Array.Empty<object>()
		// object System.Linq.Enumerable.First<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.List<object> System.Linq.Enumerable.ToList<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.NetworkMgr.<ConnectAsync>d__3>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.NetworkMgr.<ConnectAsync>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.TcpInstance.<ConnectAsync>d__18>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.TcpInstance.<ConnectAsync>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,Framework.TcpInstance.Connecter.<ConnectAsync>d__7>(System.Runtime.CompilerServices.TaskAwaiter<object>&,Framework.TcpInstance.Connecter.<ConnectAsync>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.NetworkMgr.<ConnectAsync>d__3>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.NetworkMgr.<ConnectAsync>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.TcpInstance.<ConnectAsync>d__18>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.TcpInstance.<ConnectAsync>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,Framework.TcpInstance.Connecter.<ConnectAsync>d__7>(System.Runtime.CompilerServices.TaskAwaiter<object>&,Framework.TcpInstance.Connecter.<ConnectAsync>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.NetworkMgr.<ConnectAsync>d__3>(Framework.NetworkMgr.<ConnectAsync>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.TcpInstance.<ConnectAsync>d__18>(Framework.TcpInstance.<ConnectAsync>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<Framework.TcpInstance.Connecter.<ConnectAsync>d__7>(Framework.TcpInstance.Connecter.<ConnectAsync>d__7&)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<object>(UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<object>(object)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync<object>(System.Collections.Generic.IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>,System.Action<object>)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync<object>(System.Collections.IEnumerable,System.Action<object>,UnityEngine.AddressableAssets.Addressables.MergeMode)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync<object>(object,System.Action<object>)
		// System.Void UnityEngine.AddressableAssets.Addressables.Release<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>)
		// System.Void UnityEngine.AddressableAssets.Addressables.Release<object>(object)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetAsync<object>(UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetAsync<object>(object)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetWithChain<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetWithChain<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,object)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetsAsync<object>(System.Collections.Generic.IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>,System.Action<object>,bool)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetsAsync<object>(System.Collections.IEnumerable,System.Action<object>,UnityEngine.AddressableAssets.Addressables.MergeMode,bool)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetsAsync<object>(object,System.Action<object>,bool)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetsWithChain<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,System.Collections.Generic.IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>,System.Action<object>,bool)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetsWithChain<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,System.Collections.IEnumerable,System.Action<object>,UnityEngine.AddressableAssets.Addressables.MergeMode,bool)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.AddressableAssets.AddressablesImpl.LoadAssetsWithChain<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,object,System.Action<object>,bool)
		// System.Void UnityEngine.AddressableAssets.AddressablesImpl.Release<object>(object)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.AddressableAssets.AddressablesImpl.TrackHandle<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>)
		// int UnityEngine.AndroidJNIHelper.ConvertFromJNIArray<int>(System.IntPtr)
		// object UnityEngine.AndroidJNIHelper.ConvertFromJNIArray<object>(System.IntPtr)
		// System.IntPtr UnityEngine.AndroidJNIHelper.GetFieldID<object>(System.IntPtr,string,bool)
		// System.IntPtr UnityEngine.AndroidJNIHelper.GetMethodID<int>(System.IntPtr,string,object[],bool)
		// System.IntPtr UnityEngine.AndroidJNIHelper.GetMethodID<object>(System.IntPtr,string,object[],bool)
		// object UnityEngine.AndroidJavaObject.Call<object>(string,object[])
		// int UnityEngine.AndroidJavaObject.CallStatic<int>(string,object[])
		// int UnityEngine.AndroidJavaObject.FromJavaArrayDeleteLocalRef<int>(System.IntPtr)
		// object UnityEngine.AndroidJavaObject.FromJavaArrayDeleteLocalRef<object>(System.IntPtr)
		// object UnityEngine.AndroidJavaObject.GetStatic<object>(string)
		// object UnityEngine.AndroidJavaObject._Call<object>(System.IntPtr,object[])
		// object UnityEngine.AndroidJavaObject._Call<object>(string,object[])
		// int UnityEngine.AndroidJavaObject._CallStatic<int>(System.IntPtr,object[])
		// int UnityEngine.AndroidJavaObject._CallStatic<int>(string,object[])
		// object UnityEngine.AndroidJavaObject._GetStatic<object>(System.IntPtr)
		// object UnityEngine.AndroidJavaObject._GetStatic<object>(string)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.JsonUtility.FromJson<object>(string)
		// object UnityEngine.Object.FindObjectOfType<object>()
		// object UnityEngine.Object.Instantiate<object>(object)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Convert<object>()
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.CreateChainOperation<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,System.Func<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.CreateChainOperation<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,System.Func<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>,bool)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.CreateCompletedOperation<object>(object,string)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.CreateCompletedOperationInternal<object>(object,bool,System.Exception,bool)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.CreateCompletedOperationWithException<object>(object,System.Exception)
		// object UnityEngine.ResourceManagement.ResourceManager.CreateOperation<object>(System.Type,int,UnityEngine.ResourceManagement.Util.IOperationCacheKey,System.Action<UnityEngine.ResourceManagement.AsyncOperations.IAsyncOperation>)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.ProvideResource<object>(UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<object>> UnityEngine.ResourceManagement.ResourceManager.ProvideResources<object>(System.Collections.Generic.IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>,bool,System.Action<object>)
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> UnityEngine.ResourceManagement.ResourceManager.StartOperation<object>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase<object>,UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle)
		// int UnityEngine._AndroidJNIHelper.ConvertFromJNIArray<int>(System.IntPtr)
		// object UnityEngine._AndroidJNIHelper.ConvertFromJNIArray<object>(System.IntPtr)
		// System.IntPtr UnityEngine._AndroidJNIHelper.GetFieldID<object>(System.IntPtr,string,bool)
		// System.IntPtr UnityEngine._AndroidJNIHelper.GetMethodID<int>(System.IntPtr,string,object[],bool)
		// System.IntPtr UnityEngine._AndroidJNIHelper.GetMethodID<object>(System.IntPtr,string,object[],bool)
		// string UnityEngine._AndroidJNIHelper.GetSignature<int>(object[])
		// string UnityEngine._AndroidJNIHelper.GetSignature<object>(object[])
	}
}