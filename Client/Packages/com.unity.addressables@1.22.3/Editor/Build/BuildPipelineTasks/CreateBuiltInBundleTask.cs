using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Utilities;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build.BuildPipelineTasks
{
    /// <summary>
    /// 如果Assets中资源引用了Unity内置资源，会被分别打包到各自Bundle中，造成冗余
    /// 所以这里将Unity内资资源打包
    /// </summary>
    public class CreateBuiltInBundle : IBuildTask
    {
        static readonly GUID k_BuiltInGuid = new GUID(CommonStrings.UnityBuiltInExtraGuid);
        /// <inheritdoc />
        public int Version
        {
            get
            {
                return 1;
            }
        }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)]
        IBundleExplictObjectLayout m_Layout;
#pragma warning restore 649

        public string ShaderBundleName
        {
            get; set;
        }

        public string BundleName
        {
            get; set;
        }

        public CreateBuiltInBundle(string bundleName, string shadersBundleName)
        {
            BundleName = bundleName;
            ShaderBundleName = shadersBundleName;
        }

        /// <inheritdoc />
        public ReturnCode Run()
        {
            HashSet<ObjectIdentifier> buildInObjects = new HashSet<ObjectIdentifier>();
            foreach (AssetLoadInfo dependencyInfo in m_DependencyData.AssetInfo.Values)
                buildInObjects.UnionWith(dependencyInfo.referencedObjects.Where(x => x.guid == k_BuiltInGuid));

            foreach (SceneDependencyInfo dependencyInfo in m_DependencyData.SceneInfo.Values)
                buildInObjects.UnionWith(dependencyInfo.referencedObjects.Where(x => x.guid == k_BuiltInGuid));

            ObjectIdentifier[] usedSet = buildInObjects.ToArray();
            Type[] usedTypes = BuildCacheUtility.GetMainTypeForObjects(usedSet);

            if (m_Layout == null)
                m_Layout = new BundleExplictObjectLayout();

            Type shader = typeof(Shader);
            for (int i = 0; i < usedTypes.Length; i++)
            {
                m_Layout.ExplicitObjectLocation.Add(usedSet[i], usedTypes[i] == shader ? ShaderBundleName : BundleName);
            }

            if (m_Layout.ExplicitObjectLocation.Count == 0)
                m_Layout = null;

            return ReturnCode.Success;
        }
    }
}
