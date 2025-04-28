using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Framework
{
    public interface IResourceMgr
    {
        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(IResourceLocation location);
    }
}
