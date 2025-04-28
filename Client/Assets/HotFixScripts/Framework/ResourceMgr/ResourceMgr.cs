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
    internal class ResourceMgr : IResourceMgr, FrameworkModule
    {
        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(IResourceLocation location)
        {
            return Addressables.LoadAssetAsync<TObject>(location);
        }

        public void Shutdown()
        {
            
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            
        }
    }
}
